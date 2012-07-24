// <copyright>
//   Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>

namespace Microsoft.ApplicationServer.Http.Dispatcher
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Net.Http;
    using Microsoft.ApplicationServer.Common.Test;
    using Microsoft.ApplicationServer.Http.Description;
    using Microsoft.VisualStudio.TestTools.UnitTesting;

    [TestClass, UnitTestLevel(UnitTestLevel.InProgress)]
    public class UriTemplateHandlerTests : UnitTest<UriTemplateHandler>
    {
        #region Type

        [TestMethod]
        [TestCategory("CIT"), Timeout(TimeoutConstant.DefaultTimeout), Owner("")]
        [Description("UriTemplateHandler is public and concrete.")]
        public void TypeIsCorrect()
        {
            Type t = this.TypeUnderTest;
            Assert.IsTrue(t.IsPublic, "UriTemplateHandler should be public.");
            Assert.IsFalse(t.IsAbstract, "UriTemplateHandler should not be abstract");
        }

        #endregion Type

        #region Constructors
        #endregion Constructors

        #region Properties
        #endregion Properties

        #region Methods
        #endregion Methods

        #region Test UriTemplates and Uri Strings

        private static readonly UriTemplate[] TestUriTemplates = new UriTemplate[]
            {
                new UriTemplate(""),
                new UriTemplate("/"),
                new UriTemplate("literalPath"),
                new UriTemplate("/literalPath"),
                new UriTemplate("literalPath/"),
                new UriTemplate("/literalPath/"),
                new UriTemplate("{variablePath1}"),
                new UriTemplate("/{variablePath}"),
                new UriTemplate("{variablePath1}/"),
                new UriTemplate("/{variablePath1}/"),
                new UriTemplate("/{variablePath1=default}"),
                new UriTemplate("/{variablePath1}/*"),
                new UriTemplate("*"),
                new UriTemplate("literalPath?query1={query1}"),
                new UriTemplate("literalPath/?query1={query1}"),
                new UriTemplate("{variablePath1}?query1={query1}"),
                new UriTemplate("{variablePath1}/?query1={query1}"),
                new UriTemplate("{variablePath1}/{variablePath2}?query1={query1}&query2={query2}"),
            };

        private static readonly string[] TestUriStrings = new string[]
            {
                "",
                "",
                "literalPath",
                "literalPath",
                "literalPath/",
                "literalPath/",
                "value1",
                "value1",
                "value1/",
                "value1/",
                "value1",
                "value1/*",
                "*",
                "literalPath?query1=value1",
                "literalPath/?query1=value1",
                "value1?query1=value2",
                "value1/?query1=value2",
                "value1/value2?query1=value3&query2=value4",
            };

        #endregion Test UriTemplates and Uri Strings

        #region Constructor Tests

        [TestMethod]
        [TestCategory("CIT")]
        [Timeout(TimeoutConstant.DefaultTimeout)]
        [Owner("")]
        [Description("UriTemplateHttpOperationHandler constructor throws if the 'baseAdrress' parameter is null.")]
        public void UriTemplateHttpOperationHandler_Constructor_Throws_With_Null_BaseAddress_Parameter()
        {
            ExceptionAssert.ThrowsArgumentNull("baseAddress", () => new UriTemplateHandler(null, new UriTemplate("/somePath")));
        }

        [TestMethod]
        [TestCategory("CIT")]
        [Timeout(TimeoutConstant.DefaultTimeout)]
        [Owner("")]
        [Description("UriTemplateHttpOperationHandler constructor throws if the 'uriTemplate' parameter is null.")]
        public void UriTemplateHttpOperationHandler_Constructor_Throws_With_Null_UriTemplate_Parameter()
        {
            ExceptionAssert.ThrowsArgumentNull("uriTemplate", () => new UriTemplateHandler(new Uri("http://someHost/"), null));
        }

        #endregion Constructor Tests

        #region Property Tests

        [TestMethod]
        [TestCategory("CIT")]
        [Timeout(TimeoutConstant.DefaultTimeout)]
        [Owner("")]
        [Description("UriTemplateHttpOperationHandler.BaseAddress returns the same instance as was passed in to the constructor.")]
        public void BaseAddress_Returns_Same_Instance_From_Constructor()
        {
            Uri baseAddress = new Uri("http://someHost/somePath");
            UriTemplate template = new UriTemplate("/{path1}");

            UriTemplateHandler handler = new UriTemplateHandler(baseAddress, template);

            Assert.AreSame(baseAddress, handler.BaseAddress, "UriTemplateHttpOperationHandler.BaseAddress should have returned the same instance as was passed in to the constructor.");
        }

        [TestMethod]
        [TestCategory("CIT")]
        [Timeout(TimeoutConstant.DefaultTimeout)]
        [Owner("")]
        [Description("UriTemplateHttpOperationHandler.UriTemplate returns the same instance as was passed in to the constructor.")]
        public void UriTemplate_Returns_Same_Instance_From_Constructor()
        {
            Uri baseAddress = new Uri("http://someHost/somePath");
            UriTemplate template = new UriTemplate("/{path1}");

            UriTemplateHandler handler = new UriTemplateHandler(baseAddress, template);

            Assert.AreSame(template, handler.UriTemplate, "UriTemplateHttpOperationHandler.UriTemplate should have returned the same instance as was passed in to the constructor.");
        }

        [TestMethod]
        [TestCategory("CIT")]
        [Timeout(TimeoutConstant.DefaultTimeout)]
        [Owner("")]
        [Description("UriTemplateHttpOperationHandler.InputParameters returns a collection with a single Uri HttpParameter.")]
        public void InputParameters_Is_Always_Single_Uri_Argument()
        {
            Uri baseAddress = new Uri("http://someHost/somePath");

            foreach (UriTemplate template in TestUriTemplates)
            {
                UriTemplateHandler handler = new UriTemplateHandler(baseAddress, template);

                Assert.AreEqual(1, handler.InputParameters.Count, "UriTemplateHttpOperationHandler.InputParameters.Count should have returned '1'.");
                Assert.AreEqual(HttpParameter.RequestMessage.Name, handler.InputParameters[0].Name, "UriTemplateHttpOperationHandler.InputParameters[0].Name should have returned 'RequestMessage'.");
                Assert.AreEqual(HttpParameter.RequestMessage.Type, handler.InputParameters[0].Type, "UriTemplateHttpOperationHandler.InputParameters[0].Type should have returned 'HttpRequestMessage'.");
            }
        }

        [TestMethod]
        [TestCategory("CIT")]
        [Timeout(TimeoutConstant.DefaultTimeout)]
        [Owner("")]
        [Description("UriTemplateHttpOperationHandler.OutputParameters returns a collection with the path and query variables of the UriTemplate.")]
        public void OutputParameters_With_Simple_UriTemplate()
        {
            Uri baseAddress = new Uri("http://someHost/somePath");
            UriTemplate template = new UriTemplate("somePath/{someVariable}/{someVariableWithDefault=default}?query={someQuery}");

            UriTemplateHandler handler = new UriTemplateHandler(baseAddress, template);

            Assert.AreEqual(3, handler.OutputParameters.Count, "UriTemplateHttpOperationHandler.OutputParameters.Count should have returned '3'.");
            Assert.AreEqual("SOMEVARIABLE", handler.OutputParameters[0].Name, "UriTemplateHttpOperationHandler.OutputParameters[0].Name should have returned 'SOMEVARIABLE'.");
            Assert.AreEqual(typeof(string), handler.OutputParameters[0].Type, "UriTemplateHttpOperationHandler.OutputParameters[0].Type should have returned 'string'.");
            Assert.AreEqual("SOMEVARIABLEWITHDEFAULT", handler.OutputParameters[1].Name, "UriTemplateHttpOperationHandler.OutputParameters[0].Name should have returned 'SOMEVARIABLEWITHDEFAULT'.");
            Assert.AreEqual(typeof(string), handler.OutputParameters[1].Type, "UriTemplateHttpOperationHandler.OutputParameters[0].Type should have returned 'string'.");
            Assert.AreEqual("SOMEQUERY", handler.OutputParameters[2].Name, "UriTemplateHttpOperationHandler.OutputParameters[0].Name should have returned 'SOMEQUERY'.");
            Assert.AreEqual(typeof(string), handler.OutputParameters[2].Type, "UriTemplateHttpOperationHandler.OutputParameters[0].Type should have returned 'string'.");
        }

        [TestMethod]
        [TestCategory("CIT")]
        [Timeout(TimeoutConstant.DefaultTimeout)]
        [Owner("")]
        [Description("UriTemplateHttpOperationHandler.OutputParameters does not include the wildcard of a UriTemplate as an out argument.")]
        public void OutputParameters_With_WildCard_UriTemplate()
        {
            Uri baseAddress = new Uri("http://someHost/somePath");
            UriTemplate template = new UriTemplate("somePath/{someVariable}/*?query={someQuery}");

            UriTemplateHandler handler = new UriTemplateHandler(baseAddress, template);

            Assert.AreEqual(2, handler.OutputParameters.Count, "UriTemplateHttpOperationHandler.OutputParameters.Count should have returned '3'.");
            Assert.AreEqual("SOMEVARIABLE", handler.OutputParameters[0].Name, "UriTemplateHttpOperationHandler.OutputParameters[0].Name should have returned 'SOMEVARIABLE'.");
            Assert.AreEqual(typeof(string), handler.OutputParameters[0].Type, "UriTemplateHttpOperationHandler.OutputParameters[0].Type should have returned 'string'.");
            Assert.AreEqual("SOMEQUERY", handler.OutputParameters[1].Name, "UriTemplateHttpOperationHandler.OutputParameters[0].Name should have returned 'SOMEQUERY'.");
            Assert.AreEqual(typeof(string), handler.OutputParameters[1].Type, "UriTemplateHttpOperationHandler.OutputParameters[0].Type should have returned 'string'.");
        }

        [TestMethod]
        [TestCategory("CIT")]
        [Timeout(TimeoutConstant.DefaultTimeout)]
        [Owner("")]
        [Description("UriTemplateHttpOperationHandler.OutputParameters returns a collection with the path and query variables of the UriTemplate for various UriTemplates.")]
        public void OutputParameters_With_Various_UriTemplates()
        {
            Uri baseAddress = new Uri("http://someHost/somePath");

            foreach (UriTemplate template in TestUriTemplates)
            {
                UriTemplateHandler handler = new UriTemplateHandler(baseAddress, template);

                List<string> variables = template.PathSegmentVariableNames.Concat(template.QueryValueVariableNames).ToList();
                Assert.AreEqual(variables.Count, handler.OutputParameters.Count, "UriTemplateHttpOperationHandler.OutputParameters.Count should have returned the number of variables in the UriTemplate.");

                for (int i = 0; i < variables.Count; i++)
                {
                    Assert.AreEqual(variables[i], handler.OutputParameters[i].Name, string.Format("UriTemplateHttpOperationHandler.OutputParameters[{0}].Name should have returned '{1}'.", i, variables[i]));
                    Assert.AreEqual(typeof(string), handler.OutputParameters[0].Type, string.Format("UriTemplateHttpOperationHandler.OutputParameters[{0}].Type should have returned 'string'.", i));
                }
            }
        }

        #endregion Property Tests

        #region Handle Tests

        [TestMethod]
        [TestCategory("CIT")]
        [Timeout(TimeoutConstant.DefaultTimeout)]
        [Owner("")]
        [Description("UriTemplateHttpOperationHandler.Handle returns the values from the path and query variables of the UriTemplateMatch.")]
        public void Handle_With_Simple_UriTemplate()
        {
            Uri baseAddress = new Uri("http://someHost/somePath");
            UriTemplate template = new UriTemplate("somePath/{someVariable}/{someVariableWithDefault=default}?query={someQuery}");

            UriTemplateHandler handler = new UriTemplateHandler(baseAddress, template);
            object[] result = handler.Handle(new object[] { new HttpRequestMessage(HttpMethod.Get, "http://someHost/somePath/somePath/value1/?query=value2&otherQuery=value3")});

            Assert.IsNotNull(result, "The object[] returned from Handle should not be null.");
            Assert.AreEqual(3, result.Length, "The Length of the output object array should have returned '3'.");
            Assert.AreEqual("value1", result[0], "The output object array at index 0 should have returned 'value1'.");
            Assert.AreEqual("default", result[1], "The output object array at index 1 should have returned 'default'.");
            Assert.AreEqual("value2", result[2], "The output object array at index 2 should have returned 'value2'.");
        }

        [TestMethod]
        [TestCategory("CIT")]
        [Timeout(TimeoutConstant.DefaultTimeout)]
        [Owner("")]
        [Description("UriTemplateHttpOperationHandler.Handle returns the values from the path and query variables of the UriTemplateMatch for various UriTemplates.")]
        public void Handle_With_Various_UriTemplates()
        {
            Uri baseAddress = new Uri("http://someHost/somePath/");

            for (int i = 0; i < TestUriTemplates.Length; i++)
            {
                UriTemplateHandler handler = new UriTemplateHandler(baseAddress, TestUriTemplates[i]);
                Uri uri = new Uri(baseAddress, TestUriStrings[i]);
                object[] result = handler.Handle(new object[] { new HttpRequestMessage(HttpMethod.Get, uri) });

                int expectedOutputLength = TestUriTemplates[i].PathSegmentVariableNames.Count + TestUriTemplates[i].QueryValueVariableNames.Count;
                Assert.IsNotNull(result, "The object[] returned from Handle should not be null.");
                Assert.AreEqual(expectedOutputLength, result.Length, string.Format("The length of the output object array should have returned '{0}'.", expectedOutputLength));

                for (int j = 0; j < expectedOutputLength; j++)
                {
                    string value = "value" + (j + 1).ToString();
                    Assert.AreEqual(value, result[j], string.Format("UriTemplateHttpOperationHandler.OutputParameters[{0}].Name should have returned '{1}'.", j, value));
                }
            }
        }

        [TestMethod]
        [TestCategory("CIT")]
        [Timeout(TimeoutConstant.DefaultTimeout)]
        [Owner("")]
        [Description("UriTemplateHttpOperationHandler.Handle(object[]) throws if the input value is not a Uri.")]
        public void HandleThrowsIfInputValueIsNotAUri()
        {
            Uri baseAddress = new Uri("http://someHost/somePath");
            UriTemplate template = new UriTemplate("somePath/{someVariable}/{someVariableWithDefault=default}?query={someQuery}");
            UriTemplateHandler handler = new UriTemplateHandler(baseAddress, template);
            ExceptionAssert.Throws<InvalidOperationException>(
                "UriTemplateHttpOperationHandler.Handle should throw if passed a value that is not a Uri",
                null, () => handler.Handle(new object[] { "http://localhostString" }));
        }

        [TestMethod]
        [TestCategory("CIT"), Timeout(TimeoutConstant.DefaultTimeout), Owner("")]
        [Description("UriTemplateHttpOperationHandler.Handle(object[]) throws if the Uri does not match the UriTemplate provided in the constructor.")]
        public void HandleThrowsOnFailedUriTemplateMatch()
        {
            Uri baseAddress = new Uri("http://someHost/somePath");
            UriTemplate template = new UriTemplate("somePath/{someVariable}/{someVariableWithDefault=default}?query={someQuery}");
            Uri nonMatchingUri = new Uri("http://someHost/somePath/thisPathDoesNotMatch");
            UriTemplateHandler handler = new UriTemplateHandler(baseAddress, template);

            ExceptionAssert.Throws<InvalidOperationException>(
                SR.UriTemplateDoesNotMatchUri(nonMatchingUri.ToString(), template.ToString()),
                () => handler.Handle(new object[] { new HttpRequestMessage(HttpMethod.Get, nonMatchingUri) } ));
        }

        #endregion Handle Tests
    }
}
