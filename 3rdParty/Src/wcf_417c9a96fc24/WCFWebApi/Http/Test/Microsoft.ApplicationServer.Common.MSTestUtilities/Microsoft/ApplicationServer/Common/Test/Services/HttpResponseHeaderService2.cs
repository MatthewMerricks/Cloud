// <copyright>
//   Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>

namespace Microsoft.ApplicationServer.Common.Test.Services
{
    using System.Net.Http;
    using System.Net.Http.Headers;
    using System.ServiceModel;
    using System.ServiceModel.Web;

    [ServiceContract]
    public class HttpResponseHeaderService2
    {
        // illegal because HttpResponseHeaders cannot be set
        [WebGet()]
        public string Operation(out HttpResponseHeaders headers)
        {
            headers = null;
            return null;
        }
    }
}
