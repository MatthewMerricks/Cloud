// <copyright>
//   Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>

namespace Microsoft.ApplicationServer.Http.CIT.Scenario.Common
{
    using System;
    using System.Net;
    using System.Net.Http;
    using Microsoft.ApplicationServer.Http.Dispatcher;
    using Microsoft.ApplicationServer.Http.Description;

    internal class CustomMessageFormatter : HttpMessageFormatter
    {
        private bool hasRequestMessage;
        private bool hasResponseMessage;

        public CustomMessageFormatter(HttpOperationDescription httpOperation)
        {
            if (httpOperation == null)
            {
                throw new ArgumentNullException("httpOperation");
            }

            if (httpOperation.InputParameters.Count == 1 && 
                 httpOperation.InputParameters[0].Type == typeof(HttpRequestMessage))
            {
                this.hasRequestMessage = true;
            }
            else if (httpOperation.InputParameters.Count != 0)
            {
                throw new NotSupportedException(
                    "The MessageFormatter only supports a single optional input parameter of type 'HttpRequestMessage'.");
            }

            if (httpOperation.OutputParameters.Count > 0)
            {
                throw new NotSupportedException(
                    "The MessageFormatter only does not support outputt parameters.");
            }

            if (httpOperation.ReturnValue.Type == typeof(HttpResponseMessage))
            {
                this.hasResponseMessage = true;
            }
            else if (httpOperation.ReturnValue.Type != typeof(void))
            {
                throw new NotSupportedException(
                    "The MessageFormatter only supports an optional return type of 'HttpResponseMessage'.");
            }
        }

        protected override void OnDeserializeRequest(HttpRequestMessage message, object[] parameters)
        {
            if (this.hasRequestMessage)
            {
                parameters[0] = message;
            }
        }

        protected override HttpResponseMessage OnSerializeReply(object[] parameters, object result)
        {
            if (this.hasResponseMessage)
            {
                HttpResponseMessage response = result as HttpResponseMessage;
                if (response == null)
                {
                    throw new InvalidOperationException("The result should have been an HttpResponseMessage instance.");
                }

                return response;
            }

            return new HttpResponseMessage();
        }
    }
}
