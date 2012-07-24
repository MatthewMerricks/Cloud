// <copyright>
//   Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>

namespace QueryableSample
{
    using System;
    using System.Net.Http;
    using System.Threading;

    public class TracingResponseChannel : HttpClientChannel 
    {
        private Action<HttpResponseMessage> trace;

        public TracingResponseChannel(Action<HttpResponseMessage> trace)
        {
            this.trace = trace;
        }

        protected override HttpResponseMessage Send(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            var response = base.Send(request, cancellationToken);
            this.trace(response);
            return response;
        }
    }
}