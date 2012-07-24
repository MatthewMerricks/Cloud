// <copyright>
//   Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>

namespace Microsoft.ApplicationServer.Http.Dispatcher
{
    using System;
    using System.Net;
    using System.Net.Http;
    using System.ServiceModel;
    using Microsoft.ApplicationServer.Common;
    using Microsoft.ApplicationServer.Http.Channels;

    /// <summary>
    /// An exception that allows for a given <see cref="HttpResponseMessage"/> 
    /// to be returned to the client.
    /// </summary>
    public class HttpResponseException : Exception
    {
        private const string ResponsePropertyName = "Response";

        /// <summary>
        /// Initializes a new instance of the <see cref="HttpResponseException"/> class.
        /// </summary>
        public HttpResponseException() 
            : this(HttpStatusCode.InternalServerError)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="HttpResponseException"/> class.
        /// </summary>
        /// <param name="statusCode">The status code to use with the <see cref="HttpResponseMessage"/>.</param>
        public HttpResponseException(HttpStatusCode statusCode)
            : this(new HttpResponseMessage(statusCode, null))
        {       
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="HttpResponseException"/> class.
        /// </summary>
        /// <param name="response">The response message.</param>
        public HttpResponseException(HttpResponseMessage response)
            : base(SR.HttpResponseExceptionMessage(ResponsePropertyName))
        {
            if (response == null)
            {
                throw Fx.Exception.ArgumentNull("response");
            }

            this.Response = response;

            response.RequestMessage = OperationContext.Current.GetHttpRequestMessage();
        }

        /// <summary>
        /// Gets the <see cref="HttpResponseMessage"/> to return to the client.
        /// </summary>
        public HttpResponseMessage Response { get; internal set; }
    }
}
