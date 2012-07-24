// <copyright>
//   Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>

namespace Microsoft.ApplicationServer.Http.Dispatcher
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using Microsoft.ApplicationServer.Common.Test;
    using Microsoft.ApplicationServer.Http.Description;
    using Microsoft.ApplicationServer.Http.Dispatcher;
    using Microsoft.ApplicationServer.Http.Dispatcher.Moles;
    using Microsoft.ApplicationServer.Http.Moles;
    using Microsoft.ApplicationServer.Http.Test;
    using Microsoft.VisualStudio.TestTools.UnitTesting;

    [TestClass, UnitTestLevel(UnitTestLevel.InProgress)]
    public class ResponseContentHandlerTests : UnitTest<ResponseContentHandler>
    {
        #region Type

        [TestMethod]
        [TestCategory("CIT"), Timeout(TimeoutConstant.DefaultTimeout), Owner("")]
        [Description("ResponseContentHandler is public and concrete.")]
        public void TypeIsCorrect()
        {
            Type t = this.TypeUnderTest;
            Assert.IsTrue(t.IsPublic, "ResponseContentHandler should be public.");
            Assert.IsFalse(t.IsAbstract, "ResponseContentHandler should be concrete");
        }

        #endregion Type

        #region Constructors

        [TestMethod]
        [TestCategory("CIT"), Timeout(TimeoutConstant.DefaultTimeout), Owner("")]
        [Description("ResponseContentHandler() constructor initializes parameters and formatters.")]
        public void Constructor()
        {
            HttpParameter hpd = new HttpParameter("x", typeof(int));
            HttpParameter expectedContentParameter = new HttpParameter("x", typeof(HttpResponseMessage<int>));
            SMediaTypeFormatter formatter = new SMediaTypeFormatter() { CallBase = true };
            MediaTypeFormatter[] formatters = new MediaTypeFormatter[] { formatter };
            ResponseContentHandler handler = new ResponseContentHandler(hpd, formatters);
            HttpParameterAssert.Contains(handler.InputParameters, HttpParameter.RequestMessage, "Failed to initialize input parameters for RequestMessage.");
            HttpParameterAssert.Contains(handler.InputParameters, hpd, "Failed to initialize input parameter.");
            HttpParameterAssert.ContainsOnly(handler.OutputParameters, expectedContentParameter, "Failed to initialize content parameter.");
            CollectionAssert.Contains(handler.Formatters, formatter, "Failed to accept mediaTypeFormatter.");
        }

        [Ignore]
        [TestMethod]
        [TestCategory("CIT"), Timeout(TimeoutConstant.DefaultTimeout), Owner("")]
        [Description("ResponseContentHandler() constructor throws for null Formatters.")]
        public void ConstructorThrowsWithNullHttpFormatters()
        {
            ExceptionAssert.ThrowsArgumentNull("mediaTypeFormatters", () => new ResponseContentHandler(new HttpParameter("x", typeof(int)), null));
        }

        #endregion Constructors

        #region Properties

        #region Formatters
        [TestMethod]
        [TestCategory("CIT"), Timeout(TimeoutConstant.DefaultTimeout), Owner("")]
        [Description("Formatters are initialized to the standard formatters if none are supplied.")]
        public void FormattersHaveStandardFormatters()
        {
            HttpParameter hpd = new HttpParameter("x", typeof(int));
            ResponseContentHandler handler = new ResponseContentHandler(hpd, Enumerable.Empty<MediaTypeFormatter>());
            MediaTypeFormatterCollection formatters = handler.Formatters;
            Assert.IsNotNull(formatters, "Formatters was null.");
            Assert.IsNotNull(formatters.XmlFormatter != null, "Xml formatter was not set.");
            Assert.IsNotNull(formatters.JsonFormatter != null, "Json formatter was not set.");
        }

        #endregion Formatters

        #region InputParameters

        [TestMethod]
        [TestCategory("CIT"), Timeout(TimeoutConstant.DefaultTimeout), Owner("")]
        [Description("InputParameters are created a content parameter for all legal types for content.")]
        public void InputParameterAreCreatedAllValueAndReferenceTypes()
        {
            TestDataAssert.Execute(
            HttpTestData.RepresentativeValueAndRefTypeTestDataCollection,
            TestDataVariations.All,
            "InputParameters for all types failed.",
            (type, obj) =>
            {
                Type convertType = obj == null ? type : obj.GetType();
                HttpParameter hpd = new HttpParameter("x", convertType);
                Type expectedType = typeof(HttpResponseMessage<>).MakeGenericType(convertType);
                HttpParameter expectedContentParameter = new HttpParameter("x", expectedType);
                ResponseContentHandler handler = new ResponseContentHandler(hpd, Enumerable.Empty<MediaTypeFormatter>());
                HttpParameterAssert.Contains(handler.InputParameters, HttpParameter.RequestMessage, "Failed to initialize input parameters for RequestMessage.");
                HttpParameterAssert.Contains(handler.InputParameters, hpd, "Failed to initialize input parameter.");
            });
        }

        #endregion InputParameters

        #region OutputParameters

        [TestMethod]
        [TestCategory("CIT"), Timeout(TimeoutConstant.DefaultTimeout), Owner("")]
        [Description("OutputParameters are created a content parameter for all legal types for content.")]
        public void OutputParameterAreCreatedAllValueAndReferenceTypes()
        {
            TestDataAssert.Execute(
            HttpTestData.RepresentativeValueAndRefTypeTestDataCollection,
            TestDataVariations.All,
            "OutputParameters all types failed.",
            (type, obj) =>
            {
                Type convertType = obj == null ? type : obj.GetType();
                HttpParameter hpd = new HttpParameter("x", convertType);
                Type expectedType = typeof(HttpResponseMessage<>).MakeGenericType(convertType);
                HttpParameter expectedContentParameter = new HttpParameter("x", expectedType);
                ResponseContentHandler handler = new ResponseContentHandler(hpd, Enumerable.Empty<MediaTypeFormatter>());
                HttpParameterAssert.ContainsOnly(handler.OutputParameters, expectedContentParameter, "Failed to initialize content parameter.");
            });
        }

        #endregion OutputParameters


        #endregion Properties

        #region Methods
        #endregion Methods
    }
}
