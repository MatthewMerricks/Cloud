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
    public class HttpRequestHeaderService2
    {
        // illegal because HttpRequestHeaders cannot be set
        [WebGet()]
        public string Operation(out HttpRequestHeaders headers)
        {
            headers = null;
            return null;
        }
    }
}
