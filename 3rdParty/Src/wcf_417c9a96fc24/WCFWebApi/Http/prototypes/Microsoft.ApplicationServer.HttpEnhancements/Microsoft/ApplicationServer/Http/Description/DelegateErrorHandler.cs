namespace Microsoft.ApplicationServer.Http.Description
{
    using System;
    using System.Net.Http;

    using Microsoft.ApplicationServer.Http.Dispatcher;

    internal class DelegateErrorHandler : HttpErrorHandler
    {
        private Func<Exception, bool> handleError;
        private Func<Exception, HttpResponseMessage> provideResponse;

        public DelegateErrorHandler(Func<Exception, bool> handleError, Func<Exception, HttpResponseMessage> provideResponse)
        {
            this.provideResponse = provideResponse;
            this.handleError = handleError;
        }

        protected override bool OnHandleError(Exception error)
        {
            return this.handleError(error);
        }

        protected override HttpResponseMessage OnProvideResponse(Exception error)
        {
            return this.provideResponse(error);
        }
    }
}