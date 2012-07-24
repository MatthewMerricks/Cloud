// <copyright>
// Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>

namespace Microsoft.ApplicationServer.Http.CIT.Scenario.Common
{
    using System.Net;
    using System.Net.Http;
    using System.ServiceModel;
    using System.ServiceModel.Channels;
    using Microsoft.ApplicationServer.Http;
    using Microsoft.ApplicationServer.Http.Channels;
    using Microsoft.ApplicationServer.ServiceModel;
    using Microsoft.VisualStudio.TestTools.UnitTesting;

    [ServiceBehavior(ConcurrencyMode = ConcurrencyMode.Multiple)]
    public abstract class TestServiceBase : ITestServiceContract
    {
        private const string httpReasonPhrase = "Service says hello";

        public static string HttpReasonPhrase
        {
            get
            {
                return TestServiceBase.httpReasonPhrase;
            }
        }

        public HttpRequestMessage ValidateRequest(Message request)
        {
            Assert.IsNotNull(request);
            var httpRequest = request.ToHttpRequestMessage();
            Assert.IsNotNull(httpRequest);
            return httpRequest;
        }

        public HttpResponseMessage CreateResponse()
        {
            return this.CreateResponse(HttpStatusCode.OK, TestServiceBase.HttpReasonPhrase);
        }

        public HttpResponseMessage CreateResponse(HttpStatusCode statusCode, string httpReasonPhrase)
        {
            var httpResponse = new HttpResponseMessage(statusCode, httpReasonPhrase);
            Assert.IsNotNull(httpResponse);
            Assert.AreEqual(statusCode, httpResponse.StatusCode);
            Assert.AreEqual(httpReasonPhrase, httpResponse.ReasonPhrase);
            return httpResponse;
        }

        public Message CreateMessage(HttpResponseMessage httpResponse)
        {
            var response = httpResponse.ToMessage();
            Assert.IsNotNull(response);
            return response;
        }

        public abstract Message HandleMessage(Message request);
    }
}
