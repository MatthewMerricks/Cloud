// <copyright>
//   Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>

namespace Microsoft.ApplicationServer.Http.Dispatcher
{
    using System.Net.Http;
    using Microsoft.ApplicationServer.Common;

    /// <summary>
    /// Holds all of the state of an executing <see cref="OperationHandlerPipeline"/>.
    /// </summary>
    internal class OperationHandlerPipelineContext
    {
        private object[] pipelineValues;
        private int handlerIndex;
        private OperationHandlerPipelineInfo pipelineInfo;

        internal OperationHandlerPipelineContext(OperationHandlerPipelineInfo pipelineInfo, HttpRequestMessage request)
        {
            Fx.Assert(pipelineInfo != null, "The 'pipelineInfo' parameter should not be null.");

            this.pipelineInfo = pipelineInfo;
            this.handlerIndex = 1;
            this.pipelineValues = this.pipelineInfo.GetEmptyPipelineValuesArray();
            this.pipelineInfo.SetHttpRequestMessage(request, this.pipelineValues);
        }

        internal object[] GetInputValues()
        {
            return this.pipelineInfo.GetInputValuesForHandler(this.handlerIndex, this.pipelineValues);
        }

        internal void SetOutputValuesAndAdvance(object[] values)
        {
            Fx.Assert(values != null, "The 'values' parameter shuold not be null.");

            this.pipelineInfo.SetOutputValuesFromHandler(this.handlerIndex, values, this.pipelineValues);
            this.handlerIndex++;
        }

        internal HttpResponseMessage GetHttpResponseMessage()
        {
            return this.pipelineInfo.GetHttpResponseMessage(this.pipelineValues);
        }
    }
}

