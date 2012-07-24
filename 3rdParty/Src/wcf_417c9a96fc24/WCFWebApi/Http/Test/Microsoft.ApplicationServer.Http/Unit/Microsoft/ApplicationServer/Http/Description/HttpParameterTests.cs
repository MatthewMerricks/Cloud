// <copyright>
//   Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>

namespace Microsoft.ApplicationServer.Http.Description
{
    using System;
    using System.Linq;
    using System.Net;
    using System.Net.Http;
    using System.Net.Http.Headers;
    using System.ServiceModel.Description;
    using Microsoft.ApplicationServer.Common.Test;
    using Microsoft.ApplicationServer.Common.Test.Services;
    using Microsoft.ApplicationServer.Common.Test.Types;
    using Microsoft.ApplicationServer.Http.Description;
    using Microsoft.ApplicationServer.Http.Test;
    using Microsoft.VisualStudio.TestTools.UnitTesting;

    [TestClass, UnitTestLevel(UnitTestLevel.InProgress)]
    public class HttpParameterTests : UnitTest<HttpParameter>
    {
        private static readonly string isAssignableFromParameterOfTMethodName = "IsAssignableFromParameter";

        #region Type

        [TestMethod]
        [TestCategory("CIT"), Timeout(TimeoutConstant.DefaultTimeout), Owner("")]
        [Description("HttpParameter is public, concrete and not sealed.")]
        public void TypeIsCorrect()
        {
            Type t = this.TypeUnderTest;
            Assert.IsTrue(t.IsPublic, "HttpParameter should be public.");
            Assert.IsFalse(t.IsAbstract, "HttpParameter should not be abstract.");
            Assert.IsTrue(t.IsClass, "HttpParameter should be a class.");
            Assert.IsFalse(t.IsSealed, "HttpParameter should not be sealed.");
        }

        #endregion Type

        #region Constructors

        #region HttpParameter(string, Type)

        [TestMethod]
        [TestCategory("CIT"), Timeout(TimeoutConstant.DefaultTimeout), Owner("")]
        [Description("HttpParameter(string, Type) sets Name and Type.")]
        public void Constructor()
        {
            HttpParameter hpd = new HttpParameter("aName", typeof(int));
            Assert.AreEqual("aName", hpd.Name, "Name was not set.");
            Assert.AreEqual(typeof(int), hpd.Type, "Type was not set.");
        }

        [TestMethod]
        [TestCategory("CIT"), Timeout(TimeoutConstant.DefaultTimeout), Owner("")]
        [Description("HttpParameter(string, Type) throws for empty name.")]
        public void ConstructorThrowsWithEmptyName()
        {
            foreach (string name in TestData.EmptyStrings)
            {
                ExceptionAssert.ThrowsArgumentNull("name", () => new HttpParameter(name, typeof(int)));
            }
        }

        [TestMethod]
        [TestCategory("CIT"), Timeout(TimeoutConstant.DefaultTimeout), Owner("")]
        [Description("HttpParameter(string, Type) throws for empty name.")]
        public void ConstructorThrowsWithNullType()
        {
            ExceptionAssert.ThrowsArgumentNull("type", () => new HttpParameter("aName", null));
        }

        #endregion HttpParameter(string, Type)

        #region HttpParameter(MessagePartDescription)

        [TestMethod]
        [TestCategory("CIT"), Timeout(TimeoutConstant.DefaultTimeout), Owner("")]
        [Description("HttpParameter(MessagePartDescription) (internal constructor) sets Name, Type and (internal) MessagePartDescription.")]
        public void Constructor1()
        {
            OperationDescription od = GetOperationDescription(typeof(SimpleOperationsService), "OneInputAndReturnValue");
            MessagePartDescription mpd = od.Messages[1].Body.ReturnValue;
            HttpParameter hpd = new HttpParameter(mpd);

            Assert.AreEqual("OneInputAndReturnValueResult", hpd.Name, "Name was not set correctly");
            Assert.AreEqual(typeof(string), hpd.Type, "Type was not set correctly");
            Assert.AreSame(mpd, hpd.MessagePartDescription, "Internal messagePartDescription should be what we passed to ctor");
        }

        #endregion HttpParameter(MessagePartDescription)

        #endregion Constructors

        #region Properties

        #region Name

        [TestMethod]
        [TestCategory("CIT"), Timeout(TimeoutConstant.DefaultTimeout), Owner("")]
        [Description("Name returns value from constructor.")]
        public void Name()
        {
            HttpParameter hpd = new HttpParameter("aName", typeof(char));
            Assert.AreEqual("aName", hpd.Name, "Name property was incorrect.");
        }

        #endregion Name

        #region Type

        [TestMethod]
        [TestCategory("CIT"), Timeout(TimeoutConstant.DefaultTimeout), Owner("")]
        [Description("Type returns value from constructor.")]
        public void Type()
        {
            HttpParameter hpd = new HttpParameter("aName", typeof(char));
            Assert.AreEqual(typeof(char), hpd.Type, "Type property was incorrect.");
        }

        #endregion Type

        #region IsContentParameter

        [TestMethod]
        [TestCategory("CIT"), Timeout(TimeoutConstant.DefaultTimeout), Owner("")]
        [Description("IsContentParameter returns false by default.")]
        public void IsContentParameterReturnsFalseByDefault()
        {
            HttpParameter hpd = new HttpParameter("aName", typeof(char));
            Assert.IsFalse(hpd.IsContentParameter, "IsContentParameter should have been false by default.");
        }

        [TestMethod]
        [TestCategory("CIT"), Timeout(TimeoutConstant.DefaultTimeout), Owner("")]
        [Description("IsContentParameter is mutable.")]
        public void IsContentParameterIsMutable()
        {
            HttpParameter hpd = new HttpParameter("aName", typeof(char));
            hpd.IsContentParameter = true;
            Assert.IsTrue(hpd.IsContentParameter, "IsContentParameter should have been set.");
        }

        #endregion IsContentParameter

        #region RequestMessage

        [TestMethod]
        [TestCategory("CIT"), Timeout(TimeoutConstant.DefaultTimeout), Owner("")]
        [Description("RequestMessage returns HttpRequestMessage HttpParameter.")]
        public void RequestMessageReturnsHttpRequestMessageHttpParameter()
        {
            HttpParameter hpd = HttpParameter.RequestMessage;
            Assert.IsNotNull(hpd, "RequestMessage retured null.");
            Assert.AreEqual("RequestMessage", hpd.Name, "Name was incorrect.");
            Assert.AreEqual(typeof(HttpRequestMessage), hpd.Type, "Type was incorrect.");
        }

        #endregion RequestMessage

        #region RequestUri

        [TestMethod]
        [TestCategory("CIT"), Timeout(TimeoutConstant.DefaultTimeout), Owner("")]
        [Description("RequestUri returns Uri HttpParameter.")]
        public void RequestUriReturnsUriHttpParameter()
        {
            HttpParameter hpd = HttpParameter.RequestUri;
            Assert.IsNotNull(hpd, "RequestUri retured null.");
            Assert.AreEqual("RequestUri", hpd.Name, "Name was incorrect.");
            Assert.AreEqual(typeof(Uri), hpd.Type, "Type was incorrect.");
        }

        #endregion RequestUri

        #region RequestMethod

        [TestMethod]
        [TestCategory("CIT"), Timeout(TimeoutConstant.DefaultTimeout), Owner("")]
        [Description("RequestMethod returns HttpMethod HttpParameter.")]
        public void RequestMethodReturnsHttpMethodHttpParameter()
        {
            HttpParameter hpd = HttpParameter.RequestMethod;
            Assert.IsNotNull(hpd, "RequestMethod retured null.");
            Assert.AreEqual("RequestMethod", hpd.Name, "Name was incorrect.");
            Assert.AreEqual(typeof(HttpMethod), hpd.Type, "Type was incorrect.");
        }

        #endregion RequestMethod

        #region RequestHeaders

        [TestMethod]
        [TestCategory("CIT"), Timeout(TimeoutConstant.DefaultTimeout), Owner("")]
        [Description("RequestHeaders returns HttpRequestHeaders HttpParameter.")]
        public void RequestHeadersReturnsHeadersHttpParameter()
        {
            HttpParameter hpd = HttpParameter.RequestHeaders;
            Assert.IsNotNull(hpd, "RequestHeaders retured null.");
            Assert.AreEqual("RequestHeaders", hpd.Name, "Name was incorrect.");
            Assert.AreEqual(typeof(HttpRequestHeaders), hpd.Type, "Type was incorrect.");
        }

        #endregion RequestHeaders

        #region RequestContent

        [TestMethod]
        [TestCategory("CIT"), Timeout(TimeoutConstant.DefaultTimeout), Owner("")]
        [Description("RequestHeaders returns HttpContent HttpParameter.")]
        public void RequestContentReturnsHttpContentHttpParameter()
        {
            HttpParameter hpd = HttpParameter.RequestContent;
            Assert.IsNotNull(hpd, "RequestContent retured null.");
            Assert.AreEqual("RequestContent", hpd.Name, "Name was incorrect.");
            Assert.AreEqual(typeof(HttpContent), hpd.Type, "Type was incorrect.");
        }

        #endregion RequestContent

        #region ResponseMessage

        [TestMethod]
        [TestCategory("CIT"), Timeout(TimeoutConstant.DefaultTimeout), Owner("")]
        [Description("ResponseMessage returns HttpResponseMessage HttpParameter.")]
        public void ResponseMessageReturnsHttpResponseMessageHttpParameter()
        {
            HttpParameter hpd = HttpParameter.ResponseMessage;
            Assert.IsNotNull(hpd, "ResponseMessage retured null.");
            Assert.AreEqual("ResponseMessage", hpd.Name, "Name was incorrect.");
            Assert.AreEqual(typeof(HttpResponseMessage), hpd.Type, "Type was incorrect.");
        }

        #endregion ResponseMessage

        #region ResponseStatusCode

        [TestMethod]
        [TestCategory("CIT"), Timeout(TimeoutConstant.DefaultTimeout), Owner("")]
        [Description("ResponseStatusCode returns HttpStatusCode HttpParameter.")]
        public void ResponseStatusCodeReturnsStatusCodeHttpParameter()
        {
            HttpParameter hpd = HttpParameter.ResponseStatusCode;
            Assert.IsNotNull(hpd, "ResponseStatusCode retured null.");
            Assert.AreEqual("ResponseStatusCode", hpd.Name, "Name was incorrect.");
            Assert.AreEqual(typeof(HttpStatusCode), hpd.Type, "Type was incorrect.");
        }

        #endregion ResponseStatusCode

        #region ResponseHeaders

        [TestMethod]
        [TestCategory("CIT"), Timeout(TimeoutConstant.DefaultTimeout), Owner("")]
        [Description("ResponseHeaders returns HttpResponseHeaders HttpParameter.")]
        public void ResponseHeadersReturnsHttpResponseHeadersHttpParameter()
        {
            HttpParameter hpd = HttpParameter.ResponseHeaders;
            Assert.IsNotNull(hpd, "ResponseHeaders retured null.");
            Assert.AreEqual("ResponseHeaders", hpd.Name, "Name was incorrect.");
            Assert.AreEqual(typeof(HttpResponseHeaders), hpd.Type, "Type was incorrect.");
        }

        #endregion ResponseHeaders

        #region ResponseContent

        [TestMethod]
        [TestCategory("CIT"), Timeout(TimeoutConstant.DefaultTimeout), Owner("")]
        [Description("ResponseContent returns HttpContent HttpParameter.")]
        public void ResponseContentReturnsHttpContentHttpParameter()
        {
            HttpParameter hpd = HttpParameter.ResponseContent;
            Assert.IsNotNull(hpd, "ResponseContent retured null.");
            Assert.AreEqual("ResponseContent", hpd.Name, "Name was incorrect.");
            Assert.AreEqual(typeof(HttpContent), hpd.Type, "Type was incorrect.");
        }

        #endregion ResponseContent

        #region Type (Updated via MessagePartDescription)

        [TestMethod]
        [TestCategory("CIT"), Timeout(TimeoutConstant.DefaultTimeout), Owner("")]
        [Description("Type returns value updated via MessagePartDescription.")]
        public void TypeReturnsTypeFromMessagePartDescription()
        {
            OperationDescription od = GetOperationDescription(typeof(SimpleOperationsService), "TwoInputOneOutputAndReturnValue");
            MessagePartDescription mpd = od.Messages[0].Body.Parts[0];
            HttpParameter hpd = new HttpParameter(mpd);
            mpd.Type = typeof(float);
            Assert.AreEqual(typeof(float), hpd.Type, "Setting type on messagePartDescription should update http parameter description");
        }

        #endregion Type (Updated via MessagePartDescription)

        #region ValueConverter

        [TestMethod]
        [TestCategory("CIT"), Timeout(TimeoutConstant.DefaultTimeout), Owner("")]
        [Description("ValueConverter returns a converter for all values type.")]
        public void ValueConverterReturnsConverterForAllValueTypes()
        {
            TestDataAssert.Execute(
                HttpTestData.ConvertableValueTypes,
                TestDataVariations.All,
                "ValueConverter failed",
                (type, obj) =>
                {
                    Type convertType = obj.GetType();
                    HttpParameter hpd = new HttpParameter("aName", convertType);
                    HttpParameterValueConverter converter = hpd.ValueConverter;
                    Assert.IsNotNull("ValueConverter returned null.");
                    Assert.AreEqual(convertType, converter.Type, "ValueConverter was made for wrong type.");
                });
        }

        [TestMethod]
        [TestCategory("CIT"), Timeout(TimeoutConstant.DefaultTimeout), Owner("")]
        [Description("ValueConverter returns a converter for all ObjectContent<T> types.")]
        public void ValueConverterReturnsConverterForAllObjectContentOfTTypes()
        {
            ObjectContentAssert.ExecuteForEachObjectContent(
                HttpTestData.ConvertableValueTypes,
                TestDataVariations.All,
                (objectContent, type, obj) =>
                {
                    Type convertType = objectContent.GetType();
                    HttpParameter hpd = new HttpParameter("aName", convertType);
                    HttpParameterValueConverter converter = hpd.ValueConverter;
                    Assert.IsNotNull("ValueConverter returned null.");
                    Assert.AreEqual(convertType, converter.Type, "ValueConverter was made for wrong type.");
                });
        }

        [TestMethod]
        [TestCategory("CIT"), Timeout(TimeoutConstant.DefaultTimeout), Owner("")]
        [Description("ValueConverter returns a converter for all HttpRequestMessage<T> types.")]
        public void ValueConverterReturnsConverterForAllHttpRequestMessageOfTTypes()
        {
            ObjectContentAssert.ExecuteForEachHttpRequestMessage(
                HttpTestData.ConvertableValueTypes,
                TestDataVariations.All,
                (request, type, obj) =>
                {
                    Type convertType = request.GetType();
                    HttpParameter hpd = new HttpParameter("aName", convertType);
                    HttpParameterValueConverter converter = hpd.ValueConverter;
                    Assert.IsNotNull("ValueConverter returned null.");
                    Assert.AreEqual(convertType, converter.Type, "ValueConverter was made for wrong type.");
                });
        }


        [TestMethod]
        [TestCategory("CIT"), Timeout(TimeoutConstant.DefaultTimeout), Owner("")]
        [Description("ValueConverter returns a converter for all HttpResponseMessage<T> types.")]
        public void ValueConverterReturnsConverterForAllHttpResponseMessageOfTTypes()
        {
            ObjectContentAssert.ExecuteForEachHttpResponseMessage(
                HttpTestData.ConvertableValueTypes,
                TestDataVariations.All,
                (response, type, obj) =>
                {
                    Type convertType = response.GetType();
                    HttpParameter hpd = new HttpParameter("aName", convertType);
                    HttpParameterValueConverter converter = hpd.ValueConverter;
                    Assert.IsNotNull("ValueConverter returned null.");
                    Assert.AreEqual(convertType, converter.Type, "ValueConverter was made for wrong type.");
                });
        }

        #endregion ValueConverter

        #endregion Properties

        #region Methods

        #region IsAssignableFromParameter<T>()

        [TestMethod]
        [TestCategory("CIT"), Timeout(TimeoutConstant.DefaultTimeout), Owner("")]
        [Description("IsAssignableFromParameter<T>() returns true for all values type.")]
        public void IsAssignableFromParameterOfTReturnsTrueForAllValueTypes()
        {
            TestDataAssert.Execute(
                HttpTestData.ConvertableValueTypes,
                TestDataVariations.All,
                "ValueConverter failed",
                (type, obj) =>
                {
                    Type convertType = obj.GetType();
                    HttpParameter hpd = new HttpParameter("aName", convertType);
                    bool result = (bool)GenericTypeAssert.InvokeGenericMethod(hpd, isAssignableFromParameterOfTMethodName, convertType /*, new Type[0], new object[0]*/);
                    Assert.IsTrue(result, string.Format("IsAssignableFromParameter<{0}>() was false.", convertType.Name));
                });
        }

        #endregion IsAssignableFromParameter<T>()

        #region IsAssignableFromParameter(Type)

        [TestMethod]
        [TestCategory("CIT"), Timeout(TimeoutConstant.DefaultTimeout), Owner("")]
        [Description("IsAssignableFromParameter(Type) returns true for all values type.")]
        public void IsAssignableFromParameterReturnsTrueForAllValueTypes()
        {
            TestDataAssert.Execute(
                HttpTestData.ConvertableValueTypes,
                TestDataVariations.All,
                "ValueConverter failed",
                (type, obj) =>
                {
                    Type convertType = obj.GetType();
                    HttpParameter hpd = new HttpParameter("aName", convertType);
                    Assert.IsTrue(hpd.IsAssignableFromParameter(convertType), string.Format("IsAssignableFrom({0}) was false.", convertType.Name));
                });
        }

        [TestMethod]
        [TestCategory("CIT"), Timeout(TimeoutConstant.DefaultTimeout), Owner("")]
        [Description("IsAssignableFromParameter(Type) returns true for typeof(string) for all values type.")]
        public void IsAssignableFromParameterReturnsTrueForStringForAllValueTypes()
        {
            TestDataAssert.Execute(
                HttpTestData.ConvertableValueTypes,
                TestDataVariations.AllSingleInstances,
                "ValueConverter failed",
                (type, obj) =>
                {
                    Type convertType = obj.GetType();
                    HttpParameter hpd = new HttpParameter("aName", convertType);
                    Assert.IsTrue(hpd.IsAssignableFromParameter(typeof(string)), string.Format("IsAssignableFrom({0}) was false.", convertType.Name));
                });
        }

        [TestMethod]
        [TestCategory("CIT"), Timeout(TimeoutConstant.DefaultTimeout), Owner("")]
        [Description("IsAssignableFromParameter(Type) returns false for a reference type for all values type.")]
        public void IsAssignableFromParameterReturnsFalseForReferenceTypeToValueType()
        {
            TestDataAssert.Execute(
                HttpTestData.ConvertableValueTypes,
                TestDataVariations.AllSingleInstances,
                "ValueConverter failed",
                (type, obj) =>
                {
                    Type convertType = obj.GetType();
                    HttpParameter hpd = new HttpParameter("aName", convertType);
                    Assert.IsFalse(hpd.IsAssignableFromParameter(typeof(PocoType)), string.Format("IsAssignableFrom({0}) was true.", convertType.Name));
                });
        }

        [TestMethod]
        [TestCategory("CIT"), Timeout(TimeoutConstant.DefaultTimeout), Owner("")]
        [Description("IsAssignableFromParameter(Type) returns true for all ObjectContent<T> types.")]
        public void IsAssignableFromParameterReturnsTrueForAllObjectContentOfTTypes()
        {
            ObjectContentAssert.ExecuteForEachObjectContent(
                HttpTestData.ConvertableValueTypes,
                TestDataVariations.All,
                (objectContent, type, obj) =>
                {
                    Type convertType = objectContent.GetType();
                    HttpParameter hpd = new HttpParameter("aName", convertType);
                    Assert.IsTrue(hpd.IsAssignableFromParameter(convertType), string.Format("IsAssignableFrom({0}) was false.", convertType.Name));
                });
        }

        [TestMethod]
        [TestCategory("CIT"), Timeout(TimeoutConstant.DefaultTimeout), Owner("")]
        [Description("IsAssignableFromParameter(Type) returns true for all HttpRequestMessage<T> types.")]
        public void IsAssignableFromParameterReturnsTrueForAllHttpRequestMessageOfTTypes()
        {
            ObjectContentAssert.ExecuteForEachHttpRequestMessage(
                HttpTestData.ConvertableValueTypes,
                TestDataVariations.All,
                (request, type, obj) =>
                {
                    Type convertType = request.GetType();
                    HttpParameter hpd = new HttpParameter("aName", convertType);
                    Assert.IsTrue(hpd.IsAssignableFromParameter(convertType), string.Format("IsAssignableFrom({0}) was false.", convertType.Name));
                });
        }

        [TestMethod]
        [TestCategory("CIT"), Timeout(TimeoutConstant.DefaultTimeout), Owner("")]
        [Description("IsAssignableFromParameter(Type) returns true for all HttpResponseMessage<T> types.")]
        public void IsAssignableFromParameterReturnsTrueForAllHttpResponseMessageOfTTypes()
        {
            ObjectContentAssert.ExecuteForEachHttpResponseMessage(
                HttpTestData.ConvertableValueTypes,
                TestDataVariations.All,
                (response, type, obj) =>
                {
                    Type convertType = response.GetType();
                    HttpParameter hpd = new HttpParameter("aName", convertType);
                    Assert.IsTrue(hpd.IsAssignableFromParameter(convertType), string.Format("IsAssignableFrom({0}) was false.", convertType.Name));
                });
        }

        [TestMethod]
        [TestCategory("CIT"), Timeout(TimeoutConstant.DefaultTimeout), Owner("")]
        [Description("IsAssignableFromParameter(Type) throws with a null Type.")]
        public void IsAssignableFromParameterThrowsWithNullType()
        {
             HttpParameter hpd = new HttpParameter("aName", typeof(int));
             ExceptionAssert.ThrowsArgumentNull("type", () => hpd.IsAssignableFromParameter(null));
        }

        #endregion IsAssignableFromParameter(Type)

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
