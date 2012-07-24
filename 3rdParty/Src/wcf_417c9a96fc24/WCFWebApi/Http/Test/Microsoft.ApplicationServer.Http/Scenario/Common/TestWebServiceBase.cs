// <copyright>
// Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>

namespace Microsoft.ApplicationServer.Http.CIT.Scenario.Common
{
    using System;
    using System.Net.Http;
    using System.ServiceModel;
    using System.ServiceModel.Web;
    using Microsoft.ApplicationServer.Http;

    [ServiceContract]
    public interface ITestWebServiceContract
    {
        [WebGet(UriTemplate = "")]
        HttpResponseMessage BasicOperation(HttpRequestMessage request);
    }

    [ServiceBehavior(ConcurrencyMode = ConcurrencyMode.Multiple)]
    public abstract class TestWebServiceBase : ITestWebServiceContract
    {
        private const string httpReasonPhrase = "Web Service says hello";

        private const string httpReponseContent = "Web Service says hello in body";

        public static string HttpReasonPhrase
        {
            get
            {
                return TestWebServiceBase.httpReasonPhrase;
            }
        }

        public static string HttpResponseContent
        {
            get
            {
                return TestWebServiceBase.httpReponseContent;
            }
        }

        public virtual HttpResponseMessage BasicOperation(HttpRequestMessage request)
        {
            throw new NotImplementedException();
        }
    }
}
