// <copyright>
//   Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>

namespace Microsoft.ApplicationServer.Http.CIT.Scenario.Common
{
    using System;
    using System.Net.Http;

    public class HttpResponseMessageException : Exception
    {
        public HttpResponseMessageException(HttpResponseMessage response) 
        {
            if (response == null)
            {
                throw new ArgumentNullException("response");
            }

            this.Response = response;
        }

        public HttpResponseMessage Response { get; private set; }
    }
}
