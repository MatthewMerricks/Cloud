// <copyright>
//   Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>

namespace Microsoft.ApplicationServer.Http
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Net.Http.Headers;
    using System.ServiceModel.Dispatcher;
    using Microsoft.ApplicationServer.Http.Dispatcher;
    using Microsoft.VisualStudio.TestTools.UnitTesting;
    using Microsoft.ApplicationServer.Common.Test;
    using System.Net.Http;
    using Microsoft.ApplicationServer.Http.Moles;

    [TestClass, UnitTestLevel(UnitTestLevel.InProgress), UnitTestType(typeof(MediaTypeFormatterExtensionMethods))]
    public class MediaTypeFormatterExtensionMethodsTests : UnitTest
    {
        #region Type

        [TestMethod]
        [TestCategory("CIT"), Timeout(TimeoutConstant.DefaultTimeout), Owner("")]
        [Description("MediaTypeFormatterExtensionMethods is public and static.")]
        public void TypeIsCorrect()
        {
            Type t = this.TypeUnderTest;
            Assert.IsTrue(t.IsPublic, "MediaTypeFormatterExtensionMethods should be public.");
            Assert.IsTrue(t.IsClass, "MediaTypeFormatterExtensionMethods should be a class.");
            Assert.IsTrue(t.IsAbstract, "MediaTypeFormatterExtensionMethods should be abstract.");
            Assert.IsTrue(t.IsSealed, "MediaTypeFormatterExtensionMethods should be sealed.");

            Assert.AreEqual(0, t.GetConstructors().Length, "MediaTypeFormatterExtensionMethods should be be static and have no constructors.");
        }

        #endregion Type

        #region Methods

        #region AddQueryStringMapping

        [TestMethod]
        [TestCategory("CIT"), Timeout(TimeoutConstant.DefaultTimeout), Owner("")]
        [Description("MediaTypeFormatterExtensionMethods.AddQueryStringMapping(string, string, MediaTypeHeaderValue) throws for null 'this'.")]
        public void AddQueryStringMappingThrowsWithNullThis()
        {
            MediaTypeFormatter formatter = null;
            ExceptionAssert.ThrowsArgumentNull("formatter", () => formatter.AddQueryStringMapping("name", "value", new MediaTypeHeaderValue("application/xml")));
        }

        [TestMethod]
        [TestCategory("CIT"), Timeout(TimeoutConstant.DefaultTimeout), Owner("")]
        [Description("MediaTypeFormatterExtensionMethods.AddQueryStringMapping(string, string, string) throws for null 'this'.")]
        public void AddQueryStringMapping1ThrowsWithNullThis()
        {
            MediaTypeFormatter formatter = null;
            ExceptionAssert.ThrowsArgumentNull("formatter", () => formatter.AddQueryStringMapping("name", "value", "application/xml"));
        }

        #endregion AddQueryStringMapping

        #region AddUriPathExtensionMapping

        [TestMethod]
        [TestCategory("CIT"), Timeout(TimeoutConstant.DefaultTimeout), Owner("")]
        [Description("MediaTypeFormatterExtensionMethods.AddUriPathExtensionMapping(string, MediaTypeHeaderValue) throws for null 'this'.")]
        public void AddUriPathExtensionMappingThrowsWithNullThis()
        {
            MediaTypeFormatter formatter = null;
            ExceptionAssert.ThrowsArgumentNull("formatter", () => formatter.AddUriPathExtensionMapping("xml", new MediaTypeHeaderValue("application/xml")));
        }

        [TestMethod]
        [TestCategory("CIT"), Timeout(TimeoutConstant.DefaultTimeout), Owner("")]
        [Description("MediaTypeFormatterExtensionMethods.AddUriPathExtensionMapping(string, string) throws for null 'this'.")]
        public void AddUriPathExtensionMapping1ThrowsWithNullThis()
        {
            MediaTypeFormatter formatter = null;
            ExceptionAssert.ThrowsArgumentNull("formatter", () => formatter.AddUriPathExtensionMapping("xml", "application/xml"));
        }

        #endregion AddUriPathExtensionMapping

        #region AddMediaRangeMapping

        [TestMethod]
        [TestCategory("CIT"), Timeout(TimeoutConstant.DefaultTimeout), Owner("")]
        [Description("MediaTypeFormatterExtensionMethods.AddMediaRangeMapping(MediaTypeHeaderValue, MediaTypeHeaderValue) throws for null 'this'.")]
        public void AddMediaRangeMappingThrowsWithNullThis()
        {
            MediaTypeFormatter formatter = null;
            ExceptionAssert.ThrowsArgumentNull("formatter", () => formatter.AddMediaRangeMapping(new MediaTypeHeaderValue("application/*"), new MediaTypeHeaderValue("application/xml")));
        }

        [TestMethod]
        [TestCategory("CIT"), Timeout(TimeoutConstant.DefaultTimeout), Owner("")]
        [Description("MediaTypeFormatterExtensionMethods.AddMediaRangeMapping(string, string) throws for null 'this'.")]
        public void AddMediaRangeMapping1ThrowsWithNullThis()
        {
            MediaTypeFormatter formatter = null;
            ExceptionAssert.ThrowsArgumentNull("formatter", () => formatter.AddMediaRangeMapping("application/*", "application/xml"));
        }

        #endregion AddMediaRangeMapping

        #endregion Methods
    }
}
