// <copyright>
//   Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>

namespace Microsoft.ApplicationServer.Http.Dispatcher
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Net.Http;
    using System.ServiceModel.Channels;
    using System.ServiceModel.Description;
    using Microsoft.VisualStudio.TestTools.UnitTesting;
    using Microsoft.ApplicationServer.Common.Test;
    using Microsoft.ApplicationServer.Http.Description;
    using Microsoft.ApplicationServer.Common.Test.Services;

    [TestClass]
    public class HelpPageTests
    {
        [TestMethod]
        [TestCategory("CIT")]
        [Timeout(TimeoutConstant.DefaultTimeout)]
        [Owner("")]
        [Description("Generated OperationDescription will show all UriTemplate variables as input parameters.")]
        public void UriTemplate_Variables_Test_With_WebInvoke()
        {
            ContractDescription contract = ContractDescription.GetContract(typeof(HelpPageService));
            ContractDescription contractForHelpPage = HttpBehavior.GenerateClientContractDescription(contract);
            
            Dictionary<string,Type> inputParameters = new Dictionary<string,Type>();
            inputParameters.Add("VARIABLE1", typeof(string));
            inputParameters.Add("VARIABLE2", typeof(string));
            AssertValidateParameters("WebInvokeWithTemplateStringOperation", contractForHelpPage, inputParameters, true);

            Dictionary<string, Type> outputParameters = new Dictionary<string, Type>();
            AssertValidateParameters("WebInvokeWithTemplateStringOperation", contractForHelpPage, outputParameters, false);

            AssertValidateReturnParameter("WebInvokeWithTemplateStringOperation", contractForHelpPage, typeof(void)); 
        }

        [TestMethod]
        [Description("By default help page should be disabled")]
        public void NoDuplicateHelpOperationByDefault()
        {
            string address = "http://localhost:8080/helpService";
            HttpServiceHost host = new HttpServiceHost(typeof(HelpService), new Uri(address));
            host.AddDefaultEndpoints();
            host.Open(); // this should not throw by default. see CDSMain 196212
        }

        [TestMethod]
        [TestCategory("CIT")]
        [Timeout(TimeoutConstant.DefaultTimeout)]
        [Owner("")]
        [Description("Generated OperationDescription will show all UriTemplate variables as input parameters.")]
        public void UriTemplate_Variables_Test_With_WebGet()
        {
            ContractDescription contract = ContractDescription.GetContract(typeof(HelpPageService));
            ContractDescription contractForHelpPage = HttpBehavior.GenerateClientContractDescription(contract);
            
            Dictionary<string,Type> inputParameters = new Dictionary<string,Type>();
            inputParameters.Add("variable1", typeof(string));
            AssertValidateParameters("WebGetWithTemplateStringOperationAndParameter", contractForHelpPage, inputParameters, true);

            Dictionary<string, Type> outputParameters = new Dictionary<string, Type>();
            AssertValidateParameters("WebGetWithTemplateStringOperationAndParameter", contractForHelpPage, outputParameters, false);

            AssertValidateReturnParameter("WebGetWithTemplateStringOperationAndParameter", contractForHelpPage, typeof(void)); 
        }


        [TestMethod]
        [TestCategory("CIT")]
        [Timeout(TimeoutConstant.DefaultTimeout)]
        [Owner("")]
        [Description("Generated OperationDescription will show the input variable when UriTemplate variable is differing in case")]
        public void Variables_Different_In_CaseTest()
        {
            ContractDescription contract = ContractDescription.GetContract(typeof(HelpPageService));
            ContractDescription contractForHelpPage = HttpBehavior.GenerateClientContractDescription(contract);
            
            Dictionary<string,Type> inputParameters = new Dictionary<string,Type>();
            inputParameters.Add("vArIaBlE1", typeof(string));
            AssertValidateParameters("WebGetWithTemplateStringOperationAndParameterDifferentInCase", contractForHelpPage, inputParameters, true);

            Dictionary<string, Type> outputParameters = new Dictionary<string, Type>();
            AssertValidateParameters("WebGetWithTemplateStringOperationAndParameterDifferentInCase", contractForHelpPage, outputParameters, false);

            AssertValidateReturnParameter("WebGetWithTemplateStringOperationAndParameterDifferentInCase", contractForHelpPage, typeof(void)); 
        }

        [TestMethod]
        [TestCategory("CIT")]
        [Timeout(TimeoutConstant.DefaultTimeout)]
        [Owner("")]
        [Description("Generated OperationDescription will only show the uritemplate variables when operation parameter is not matching")]
        public void WebGet_With_Template_Parameter_Mismatch_Test()
        {
            ContractDescription contract = ContractDescription.GetContract(typeof(HelpPageService));
            ContractDescription contractForHelpPage = HttpBehavior.GenerateClientContractDescription(contract);
            
            Dictionary<string,Type> inputParameters = new Dictionary<string,Type>();
            inputParameters.Add("X", typeof(string));
            inputParameters.Add("Y", typeof(string));
            AssertValidateParameters("WebGetWithTemplateAndParameterNotMatching", contractForHelpPage, inputParameters, true);

            Dictionary<string,Type> outputParameters = new Dictionary<string,Type>();
            AssertValidateParameters("WebGetWithTemplateAndParameterNotMatching", contractForHelpPage, outputParameters, false);

            AssertValidateReturnParameter("WebGetWithTemplateAndParameterNotMatching", contractForHelpPage, typeof(void)); 
        }

        [TestMethod]
        [TestCategory("CIT")]
        [Timeout(TimeoutConstant.DefaultTimeout)]
        [Owner("")]
        [Description("When HttpRequestMessage is an in parameter in a WebGet operation, it should not be in the input parameter list of the generated OperationDescription.")]
        public void WebGet_With_Default_UriTemplate_And_HttpRequest_As_Parameter_Test()
        {
            ContractDescription contract = ContractDescription.GetContract(typeof(HelpPageService));
            ContractDescription contractForHelpPage = HttpBehavior.GenerateClientContractDescription(contract);
           
            Dictionary<string,Type> inputParameters = new Dictionary<string,Type>();
            inputParameters.Add("request", typeof(HttpRequestMessage));
            AssertValidateParameters("WebGetWithHttpRequestAsParameter", contractForHelpPage, inputParameters, true);

            Dictionary<string,Type> outputParameters = new Dictionary<string,Type>();
            AssertValidateParameters("WebGetWithHttpRequestAsParameter", contractForHelpPage, outputParameters, false);

            AssertValidateReturnParameter("WebGetWithHttpRequestAsParameter", contractForHelpPage, typeof(void)); 
        }


        [TestMethod]
        [TestCategory("CIT")]
        [Timeout(TimeoutConstant.DefaultTimeout)]
        [Owner("")]
        [Description("Test for HttpRequestMessage and HttpResponseMessage as operation parameters.")]
        public void WebGet_HttpRequest_And_HttpResponse_As_Parameters_Test()
        {
            ContractDescription contract = ContractDescription.GetContract(typeof(HelpPageService));
            ContractDescription contractForHelpPage = HttpBehavior.GenerateClientContractDescription(contract);
            
            Dictionary<string,Type> inputParameters = new Dictionary<string,Type>();
            AssertValidateParameters("WebGetWithHttpRequestResponseParameters", contractForHelpPage, inputParameters, true);

            Dictionary<string,Type> outputParameters = new Dictionary<string,Type>();
            AssertValidateParameters("WebGetWithHttpRequestResponseParameters", contractForHelpPage, outputParameters, false);

            AssertValidateReturnParameter("WebGetWithHttpRequestResponseParameters", contractForHelpPage, typeof(Message)); 
        }


        [TestMethod]
        [TestCategory("CIT")]
        [Timeout(TimeoutConstant.DefaultTimeout)]
        [Owner("")]
        [Description("Test for HttpRequestMessage and HttpResponseMessage as parameters to a WebInvoke operation.")]
        public void WebInvoke_HttpRequest_And_HttpResponse_As_Parameters_Test()
        {
            ContractDescription contract = ContractDescription.GetContract(typeof(HelpPageService));
            ContractDescription contractForHelpPage = HttpBehavior.GenerateClientContractDescription(contract);

            Dictionary<string, Type> inputParameters = new Dictionary<string, Type>();
            inputParameters.Add("request", typeof(Message));
            AssertValidateParameters("WebInvokeWithHttpRequestResponseParameters", contractForHelpPage, inputParameters, true);

            Dictionary<string, Type> outputParameters = new Dictionary<string, Type>();
            AssertValidateParameters("WebInvokeWithHttpRequestResponseParameters", contractForHelpPage, outputParameters, false);

            AssertValidateReturnParameter("WebInvokeWithHttpRequestResponseParameters", contractForHelpPage, typeof(Message));
        }

        [TestMethod]
        [TestCategory("CIT")]
        [Timeout(TimeoutConstant.DefaultTimeout)]
        [Owner("")]
        [Description("Test for HttpRequestMessage and HttpResponseMessage as out parameters to a WebGet operation.")]
        public void WebGet_HttpRequest_And_HttpResponse_As_OutParameters_Test()
        {
            ContractDescription contract = ContractDescription.GetContract(typeof(HelpPageService));
            ContractDescription contractForHelpPage = HttpBehavior.GenerateClientContractDescription(contract);

            Dictionary<string, Type> inputParameters = new Dictionary<string, Type>();
            AssertValidateParameters("WebGetWithHttpRequestResponseAsOutParameters", contractForHelpPage, inputParameters, true);

            Dictionary<string, Type> outputParameters = new Dictionary<string, Type>();
            AssertValidateParameters("WebGetWithHttpRequestResponseAsOutParameters", contractForHelpPage, outputParameters, false);

            AssertValidateReturnParameter("WebGetWithHttpRequestResponseAsOutParameters", contractForHelpPage, typeof(Message));
        }

        // Helper to validate input and parameters
        private void AssertValidateParameters(string operationName, ContractDescription contractForHelpPage, Dictionary<string, Type> parameters, bool isInput)
        {
            OperationDescription operationDescription = contractForHelpPage.Operations.Where(od => od.Name == operationName).FirstOrDefault();
            HttpOperationDescription httpOperationDescription = operationDescription.ToHttpOperationDescription();
            HttpParameter[] httpParameterDescriptions;
            if (isInput)
            {
                 httpParameterDescriptions = httpOperationDescription.InputParameters.ToArray();
            }
            else
            {
                httpParameterDescriptions = httpOperationDescription.OutputParameters.ToArray();
            }
            Assert.AreEqual(parameters.Count, httpParameterDescriptions.Length, "Operation " + operationName + " found incorrect number of " + (isInput? "input" : "output") + " parameters");

            for (int i = 0; i < httpParameterDescriptions.Length; i++)
            {
                Type type;
                if (!parameters.TryGetValue(httpParameterDescriptions[i].Name, out type))
                {
                    Assert.Fail("Expected {0} parameter {1} not found in operation {2}", isInput? "input" : "output", httpParameterDescriptions[i].Name, operationName);
                }
                if (type != httpParameterDescriptions[i].Type)
                {
                    Assert.Fail("Type mismatch for {0} parameter {1} in operation {2}, Found {3}, Expected {4}", isInput ? "input" : "output", httpParameterDescriptions[i].Name, operationName, httpParameterDescriptions[i].Type, type);
                }
            }
        }

        // Helper to return Type
        private void AssertValidateReturnParameter(string operationName, ContractDescription contractForHelpPage, Type returnType)
        {
            OperationDescription operationDescription = contractForHelpPage.Operations.Where(od => od.Name == operationName).FirstOrDefault();
            HttpOperationDescription httpOperationDescription = operationDescription.ToHttpOperationDescription();

            HttpParameter returnParameter = httpOperationDescription.ReturnValue;
            Assert.AreEqual(returnType, returnParameter.Type, "Return type is not matching");
        }
    }
    
}
