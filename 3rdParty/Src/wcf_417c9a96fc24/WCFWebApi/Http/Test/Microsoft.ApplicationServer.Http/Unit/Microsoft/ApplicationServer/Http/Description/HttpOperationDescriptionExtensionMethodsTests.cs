// <copyright>
//   Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>

namespace Microsoft.ApplicationServer.Http.Description
{
    using System;
    using System.Linq;
    using System.Net.Http;
    using System.ServiceModel.Description;
    using System.ServiceModel.Web;
    using Microsoft.VisualStudio.TestTools.UnitTesting;
    using Microsoft.ApplicationServer.Common.Test;
    using Microsoft.ApplicationServer.Http.Description;
    using Microsoft.ApplicationServer.Common.Test.Services;

    [TestClass, UnitTestLevel(UnitTestLevel.InProgress), UnitTestType(typeof(HttpOperationDescriptionExtensionMethods))]
    public class HttpOperationDescriptionExtensionMethodsTests : UnitTest
    {
        #region Type

        [TestMethod]
        [TestCategory("CIT"), Timeout(TimeoutConstant.DefaultTimeout), Owner("")]
        [Description("HttpOperationDescriptionExtensionMethods is a class that is public, abstract, sealed (static).")]
        public void TypeIsCorrect()
        {
            Type t = this.TypeUnderTest;
            Assert.IsTrue(t.IsPublic, "HttpOperationDescriptionExtensionMethods should be public.");
            Assert.IsTrue(t.IsAbstract, "HttpOperationDescriptionExtensionMethods should be static.");
            Assert.IsTrue(t.IsClass, "HttpOperationDescriptionExtensionMethods should be a class.");
            Assert.IsTrue(t.IsSealed, "HttpOperationDescriptionExtensionMethods should be sealed.");
        }

        #endregion Type

        #region Methods

        #region HttpParameter


        [TestMethod]
        [TestCategory("CIT")]
        [Timeout(TimeoutConstant.DefaultTimeout)]
        [Owner("")]
        [Description("HttpParameter can be constructed via extension method from MessagePartDescription.")]
        public void HttpParameter_ExtensionMethod_Creates_From_MessagePartDescription()
        {
            OperationDescription od = GetOperationDescription(typeof(SimpleOperationsService), "OneInputAndReturnValue");
            MessagePartDescription mpd = od.Messages[1].Body.ReturnValue;
            HttpParameter hpd = mpd.ToHttpParameter();

            Assert.AreEqual("OneInputAndReturnValueResult", hpd.Name, "Name was not set correctly");
            Assert.AreEqual(typeof(string), hpd.Type, "Type was not set correctly");
            Assert.AreSame(mpd, hpd.MessagePartDescription, "Internal messagePartDescription should be what we passed to ctor");
        }

        [TestMethod]
        [TestCategory("CIT")]
        [Timeout(TimeoutConstant.DefaultTimeout)]
        [Owner("")]
        [Description("HttpParameter can be constructed via extension method from MessagePartDescription.")]
        public void HttpParameter_ExtensionMethod_Throws_Null_MessagePartDescription()
        {
            MessagePartDescription mpd = null;
            ExceptionAssert.ThrowsArgumentNull(
                "description",
                () => mpd.ToHttpParameter()
            );
        }

        #endregion HttpParameter


        [TestMethod]
        [TestCategory("CIT")]
        [Timeout(TimeoutConstant.DefaultTimeout)]
        [Owner("")]
        [Description("HttpOperationDescription.GetHttpMethod returns the correct HttpMethod for the operation.")]
        public void GetHttpMethod_Returns_HttpMethod()
        {
            ExceptionAssert.ThrowsArgumentNull("operation", () => ((HttpOperationDescription)null).GetHttpMethod());

            ContractDescription contract = ContractDescription.GetContract(typeof(WebMethodService));
            
            OperationDescription operationDescription = contract.Operations.Where(od => od.Name == "NoAttributeOperation").FirstOrDefault();
            HttpOperationDescription httpOperationDescription = operationDescription.ToHttpOperationDescription();
            Assert.AreEqual(HttpMethod.Post, httpOperationDescription.GetHttpMethod(), "HttpOperationDescription.GetHttpMethod should return 'POST' for operations with no WebGet or WebInvoke attribute.");

            operationDescription = contract.Operations.Where(od => od.Name == "WebInvokeOperation").FirstOrDefault();
            httpOperationDescription = operationDescription.ToHttpOperationDescription();
            Assert.AreEqual(HttpMethod.Post, httpOperationDescription.GetHttpMethod(), "HttpOperationDescription.GetHttpMethod should return 'POST' for operations with WebInvoke attribute but no Method set explicitly.");

            operationDescription = contract.Operations.Where(od => od.Name == "WebGetOperation").FirstOrDefault();
            httpOperationDescription = operationDescription.ToHttpOperationDescription();
            Assert.AreEqual(HttpMethod.Get, httpOperationDescription.GetHttpMethod(), "HttpOperationDescription.GetHttpMethod should return 'GET' for operations with WebGet.");

            operationDescription = contract.Operations.Where(od => od.Name == "WebInvokeGetOperation").FirstOrDefault();
            httpOperationDescription = operationDescription.ToHttpOperationDescription();
            Assert.AreEqual(HttpMethod.Get, httpOperationDescription.GetHttpMethod(), "HttpOperationDescription.GetHttpMethod should have return 'GET'.");

            operationDescription = contract.Operations.Where(od => od.Name == "WebInvokeGetLowerCaseOperation").FirstOrDefault();
            httpOperationDescription = operationDescription.ToHttpOperationDescription();
            Assert.AreEqual(new HttpMethod("Get"), httpOperationDescription.GetHttpMethod(), "HttpOperationDescription.GetHttpMethod should have return 'Get'.");

            operationDescription = contract.Operations.Where(od => od.Name == "WebInvokePutOperation").FirstOrDefault();
            httpOperationDescription = operationDescription.ToHttpOperationDescription();
            Assert.AreEqual(HttpMethod.Put, httpOperationDescription.GetHttpMethod(), "HttpOperationDescription.GetHttpMethod should have return 'PUT'.");

            operationDescription = contract.Operations.Where(od => od.Name == "WebInvokePostOperation").FirstOrDefault();
            httpOperationDescription = operationDescription.ToHttpOperationDescription();
            Assert.AreEqual(HttpMethod.Post, httpOperationDescription.GetHttpMethod(), "HttpOperationDescription.GetHttpMethod should have return 'POST'.");

            operationDescription = contract.Operations.Where(od => od.Name == "WebInvokeDeleteOperation").FirstOrDefault();
            httpOperationDescription = operationDescription.ToHttpOperationDescription();
            Assert.AreEqual(HttpMethod.Delete, httpOperationDescription.GetHttpMethod(), "HttpOperationDescription.GetHttpMethod should have return 'DELETE'.");

            operationDescription = contract.Operations.Where(od => od.Name == "WebInvokeCustomOperation").FirstOrDefault();
            httpOperationDescription = operationDescription.ToHttpOperationDescription();
            Assert.AreEqual(new HttpMethod("Custom"), httpOperationDescription.GetHttpMethod(), "HttpOperationDescription.GetHttpMethod should have return 'Custom'.");
        }

        [TestMethod]
        [TestCategory("CIT")]
        [Timeout(TimeoutConstant.DefaultTimeout)]
        [Owner("")]
        [Description("HttpOperationDescription.GetUriTemplate returns the correct UriTemplate for the operation.")]
        public void GetUriTemplate_Returns_UriTemplate()
        {
            ExceptionAssert.ThrowsArgumentNull("operation", () => ((HttpOperationDescription)null).GetUriTemplate());

            ContractDescription contract = ContractDescription.GetContract(typeof(UriTemplateService));

            OperationDescription operationDescription = contract.Operations.Where(od => od.Name == "NoAttributeOperation").FirstOrDefault();
            HttpOperationDescription httpOperationDescription = operationDescription.ToHttpOperationDescription();
            UriTemplate template = httpOperationDescription.GetUriTemplate();
            Assert.AreEqual(0, template.PathSegmentVariableNames.Count , "HttpOperationDescription.GetUriTemplate should return a UriTemplate with zero path variables.");
            Assert.AreEqual(0, template.QueryValueVariableNames.Count, "HttpOperationDescription.GetUriTemplate should return a UriTemplate with zero query variables.");

            operationDescription = contract.Operations.Where(od => od.Name == "WebInvokeSansTemplateStringOperation").FirstOrDefault();
            httpOperationDescription = operationDescription.ToHttpOperationDescription();
            template = httpOperationDescription.GetUriTemplate();
            Assert.AreEqual(0, template.PathSegmentVariableNames.Count, "HttpOperationDescription.GetUriTemplate should return a UriTemplate with zero path variables.");
            Assert.AreEqual(0, template.QueryValueVariableNames.Count, "HttpOperationDescription.GetUriTemplate should return a UriTemplate with zero query variables.");

            operationDescription = contract.Operations.Where(od => od.Name == "WebGetSansTemplateStringOperation").FirstOrDefault();
            httpOperationDescription = operationDescription.ToHttpOperationDescription();
            template = httpOperationDescription.GetUriTemplate();
            Assert.AreEqual(0, template.PathSegmentVariableNames.Count, "HttpOperationDescription.GetUriTemplate should return a UriTemplate with zero path variables.");
            Assert.AreEqual(0, template.QueryValueVariableNames.Count, "HttpOperationDescription.GetUriTemplate should return a UriTemplate with zero query variables.");

            operationDescription = contract.Operations.Where(od => od.Name == "WebInvokeWithParametersOperation").FirstOrDefault();
            httpOperationDescription = operationDescription.ToHttpOperationDescription();
            template = httpOperationDescription.GetUriTemplate();
            Assert.AreEqual(0, template.PathSegmentVariableNames.Count, "HttpOperationDescription.GetUriTemplate should return a UriTemplate with zero path variables.");
            Assert.AreEqual(0, template.QueryValueVariableNames.Count, "HttpOperationDescription.GetUriTemplate should return a UriTemplate with zero query variables.");

            operationDescription = contract.Operations.Where(od => od.Name == "WebGetWithParametersOperation").FirstOrDefault();
            httpOperationDescription = operationDescription.ToHttpOperationDescription();
            template = httpOperationDescription.GetUriTemplate();
            Assert.AreEqual(0, template.PathSegmentVariableNames.Count, "HttpOperationDescription.GetUriTemplate should return a UriTemplate with zero path variables.");
            Assert.AreEqual(2, template.QueryValueVariableNames.Count, "HttpOperationDescription.GetUriTemplate should return a UriTemplate with two query variables.");
            Assert.AreEqual("IN1", template.QueryValueVariableNames[0], "HttpOperationDescription.GetUriTemplate should return a UriTemplate with query variable 'IN1'.");
            Assert.AreEqual("IN2", template.QueryValueVariableNames[1], "HttpOperationDescription.GetUriTemplate should return a UriTemplate with query variable 'IN2'.");

            operationDescription = contract.Operations.Where(od => od.Name == "WebInvokeWithEmptyTemplateStringOperation").FirstOrDefault();
            httpOperationDescription = operationDescription.ToHttpOperationDescription();
            template = httpOperationDescription.GetUriTemplate();
            Assert.AreEqual(0, template.PathSegmentVariableNames.Count, "HttpOperationDescription.GetUriTemplate should return a UriTemplate with zero path variables.");
            Assert.AreEqual(0, template.QueryValueVariableNames.Count, "HttpOperationDescription.GetUriTemplate should return a UriTemplate with zero query variables.");

            operationDescription = contract.Operations.Where(od => od.Name == "WebInvokeWithTemplateStringOperation").FirstOrDefault();
            httpOperationDescription = operationDescription.ToHttpOperationDescription();
            template = httpOperationDescription.GetUriTemplate();
            Assert.AreEqual(1, template.PathSegmentVariableNames.Count, "HttpOperationDescription.GetUriTemplate should return a UriTemplate with one path variables.");
            Assert.AreEqual(1, template.QueryValueVariableNames.Count, "HttpOperationDescription.GetUriTemplate should return a UriTemplate with one query variables.");
            Assert.AreEqual("VARIABLE1", template.PathSegmentVariableNames[0], "HttpOperationDescription.GetUriTemplate should return a UriTemplate with query variable 'VARIABLE1'.");
            Assert.AreEqual("VARIABLE2", template.QueryValueVariableNames[0], "HttpOperationDescription.GetUriTemplate should return a UriTemplate with query variable 'VARIABLE2'.");

            operationDescription = contract.Operations.Where(od => od.Name == "WebGetWithTemplateStringOperation").FirstOrDefault();
            httpOperationDescription = operationDescription.ToHttpOperationDescription();
            template = httpOperationDescription.GetUriTemplate();
            Assert.AreEqual(1, template.PathSegmentVariableNames.Count, "HttpOperationDescription.GetUriTemplate should return a UriTemplate with one path variables.");
            Assert.AreEqual(0, template.QueryValueVariableNames.Count, "HttpOperationDescription.GetUriTemplate should return a UriTemplate with zero query variables.");
            Assert.AreEqual("VARIABLE1", template.PathSegmentVariableNames[0], "HttpOperationDescription.GetUriTemplate should return a UriTemplate with query variable 'VARIABLE1'.");
        }

        #endregion Methods

        #region Test helpers

        public static OperationDescription GetOperationDescription(Type contractType, string methodName)
        {
            ContractDescription cd = ContractDescription.GetContract(contractType);
            OperationDescription od = cd.Operations.FirstOrDefault(o => o.Name.Equals(methodName, StringComparison.OrdinalIgnoreCase));
            Assert.IsNotNull(od, "Failed to get operation description for " + methodName);
            return od;
        }

        #endregion Test helpers
    }
}
