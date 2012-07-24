// <copyright>
//   Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>

namespace Microsoft.ApplicationServer.Http.CIT.Scenario.Common
{
    using System;
    using System.Net.Http;
    using System.ServiceModel.Dispatcher;
    using Microsoft.ApplicationServer.Http.Dispatcher;

    internal class CustomErrorHandler : HttpErrorHandler
    {
        protected override bool OnHandleError(Exception error)
        {
            if (error == null)
            {
                throw new ArgumentNullException("error");
            }

            return error is HttpResponseMessageException;
        }

        protected override HttpResponseMessage OnProvideResponse(Exception error)
        {
            if (error == null)
            {
                throw new ArgumentNullException("error");
            }

            HttpResponseMessageException httpError = error as HttpResponseMessageException;
            return httpError != null ? httpError.Response : new HttpResponseMessage();
        }
    }
}
