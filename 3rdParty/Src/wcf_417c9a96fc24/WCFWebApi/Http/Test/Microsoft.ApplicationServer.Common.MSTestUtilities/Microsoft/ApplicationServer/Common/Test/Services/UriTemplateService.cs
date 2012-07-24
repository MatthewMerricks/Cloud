// <copyright>
//   Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>

namespace Microsoft.ApplicationServer.Common.Test.Services
{
    using System;
    using System.ServiceModel;
    using System.ServiceModel.Web;

    [ServiceContract]
    public class UriTemplateService
    {
        [OperationContract]
        public void NoAttributeOperation()
        {
        }

        [WebInvoke]
        public void WebInvokeSansTemplateStringOperation()
        {
        }

        [WebGet]
        public void WebGetSansTemplateStringOperation()
        {
        }

        [WebInvoke]
        public void WebInvokeWithParametersOperation(string in1, int in2, out int out1)
        {
            throw new NotImplementedException();
        }

        [WebGet]
        public void WebGetWithParametersOperation(string in1, int in2, out int out1)
        {
            throw new NotImplementedException();
        }

        [WebInvoke(UriTemplate = "")]
        public void WebInvokeWithEmptyTemplateStringOperation()
        {
        }

        [WebGet(UriTemplate = "/")]
        public void WebGetWithFowardSlashTemplateStringOperation()
        {
        }

        [WebInvoke(UriTemplate = "{variable1}?param={variable2}")]
        public void WebInvokeWithTemplateStringOperation()
        {
        }

        [WebGet(UriTemplate = "{variable1}")]
        public void WebGetWithTemplateStringOperation()
        {
        }
    }
}
