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
    public class BodyParameterService
    {
        [WebInvoke(UriTemplate = "{variable1}?param={variable2}")]
        public void WebInvokeWithTemplateStringOperation()
        {
        }

        [WebGet(UriTemplate = "{variable1}")]
        public void WebGetWithTemplateStringOperation()
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

        [WebGet(UriTemplate = "{variable1}")]
        public void WebGetWithTemplateStringOperationAndNonMatchingParameter(string notVariable1)
        {
        }

        [WebGet(UriTemplate = "{variable1}")]
        public void WebGetWithTemplateStringOperationAndOutParameter(out string variable1)
        {
            variable1 = null;
        }

        [WebInvoke()]
        public void WebInvokeWithMultipleBodyParameters(string param1, string param2)
        {  
        }

        [WebInvoke(UriTemplate = "{variable1}")]
        public void WebInvokeWithTemplateStringOperationMatchingParameter(string variable1, out string variable2)
        {
            variable2 = null;
        }

        [WebInvoke(UriTemplate = "{variable1}")]
        public void WebInvokeWithTemplateStringOperationMatchingInParameter(string variable1, string variable2)
        {
        }

        [WebInvoke(UriTemplate = "{variable1}")]
        public void WebInvokeWithTemplateStringOperationMatchingInParameterMixedCase(string variable1, string vArIaBlE2)
        {
        }

        [WebInvoke(UriTemplate = "{variable1}", BodyStyle = WebMessageBodyStyle.WrappedRequest)]
        public void WebInvokeWithTemplateStringOperationInParameterWrappedRequest(string variable1, string variable2)
        {
            variable2 = null;
        }

        [WebInvoke(UriTemplate = "{variable1}")]
        public void WebInvokeWithTemplateStringOperationMatchingParameterMixedCase(string variable1, out string vArIaBlE2)
        {
            vArIaBlE2 = null;
        }

        [WebInvoke(UriTemplate = "{variable1}", BodyStyle = WebMessageBodyStyle.WrappedRequest)]
        public void WebInvokeWithTemplateStringOperationOutParameterWrappedRequest(string variable1, out string variable2)
        {
            variable2 = null;
        }

        [WebInvoke(UriTemplate = "{variable1}", BodyStyle = WebMessageBodyStyle.WrappedRequest)]
        public void WebInvokeWithTemplateStringOperationOutParameterWrappedRequestNotBody(string variable1, out HttpRequestMessage variable2)
        {
            variable2 = null;
        }


        [WebGet]
        public void WebGetWithInParameter(string variable1)
        {
        }

        [WebGet]
        public void WebGetWithOutParameter(out string variable1)
        {
            variable1 = null;
        }

        [WebGet]
        public string WebGetWithOutParameterAndReturn(out string variable1)
        {
            variable1 = null;
            return null;
        }

        [WebGet]
        public void WebGetWithHttpOutParameters(out HttpRequestMessage request, out HttpResponseMessage response)
        {
            request = null;
            response = null;
        }

    }
}
