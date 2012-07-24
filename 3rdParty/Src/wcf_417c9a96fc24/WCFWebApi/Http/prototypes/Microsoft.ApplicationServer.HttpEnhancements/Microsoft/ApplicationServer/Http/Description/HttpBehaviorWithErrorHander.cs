using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using Microsoft.ApplicationServer.Http.Description;
using Microsoft.ApplicationServer.Http.Dispatcher;

namespace Microsoft.ApplicationServer.Http.Description
{
    public class HttpBehaviorWithErrorHandler : HttpBehavior
    {
        private readonly HttpErrorHandler httpErrorHandler;

        public HttpBehaviorWithErrorHandler(HttpErrorHandler httpErrorHandler)
        {
            this.httpErrorHandler = httpErrorHandler;
        }

        protected override IEnumerable<global::Microsoft.ApplicationServer.Http.Dispatcher.HttpErrorHandler> OnGetHttpErrorHandlers(System.ServiceModel.Description.ServiceEndpoint endpoint, IEnumerable<HttpOperationDescription> operations)
        {
            yield return this.httpErrorHandler;
        }
    }
}
