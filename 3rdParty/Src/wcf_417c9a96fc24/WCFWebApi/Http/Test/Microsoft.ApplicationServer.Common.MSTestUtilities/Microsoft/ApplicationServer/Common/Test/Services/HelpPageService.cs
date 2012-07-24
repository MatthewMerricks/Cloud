// <copyright>
//   Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>

namespace Microsoft.ApplicationServer.Common.Test.Services
{
    using System;
    using System.Net.Http;
    using System.ServiceModel;
    using System.ServiceModel.Web;

    [ServiceContract]
    public class HelpPageService
    {
        [WebInvoke(UriTemplate = "{variable1}?param={variable2}")]
        public void WebInvokeWithTemplateStringOperation()
        {
        }

        [WebGet(UriTemplate = "{variable1}")]
        public void WebGetWithTemplateStringOperationAndParameter(string variable1)
        {
        }

        [WebGet(UriTemplate = "{variable1}")]
        public void WebGetWithTemplateStringOperationAndParameterDifferentInCase(string vArIaBlE1)
        {
        }

        [WebGet(UriTemplate = "{x}/{y}")]
        public void WebGetWithTemplateAndParameterNotMatching(Point p)
        {
        }

        // default UriTemplate
        [WebGet]
        public void WebGetWithHttpRequestAsParameter(HttpRequestMessage request)
        {
            request = null;
        }

        [WebGet(UriTemplate = "WebGetWithHttpRequestResponseParameters")]
        public void WebGetWithHttpRequestResponseParameters(HttpRequestMessage request, HttpResponseMessage response)
        {
            request = null;
            response = null;
        }

        [WebInvoke(UriTemplate = "")]
        public void WebInvokeWithHttpRequestResponseParameters(HttpRequestMessage request, HttpResponseMessage response)
        {
            request = null;
            response = null;
        }

        [WebGet]
        public void WebGetWithHttpRequestResponseAsOutParameters(out HttpRequestMessage request, out HttpResponseMessage response)
        {
            request = null;
            response = null;
        }
    }

    public class Point
    {
        public Point(string x, string y)
        {
            this.X = x;
            this.Y = y;
        }

        string X { get; set; }
        string Y { get; set; }
    }

    [ServiceContract]
    public class HelpService
    {
        [OperationContract]
        [WebGet(UriTemplate = "HELP")]
        public string HelpAsUriTemplate()
        {
            return "HelpAsUriTemplate";
        }

    }
}
