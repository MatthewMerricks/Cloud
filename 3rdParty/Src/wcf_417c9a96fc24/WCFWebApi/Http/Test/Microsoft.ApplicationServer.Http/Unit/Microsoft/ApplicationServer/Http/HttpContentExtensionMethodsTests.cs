// <copyright>
//   Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>

namespace Microsoft.ApplicationServer.Http
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Net.Http;
    using System.Net.Http.Headers;
    using System.ServiceModel.Dispatcher;
    using Microsoft.ApplicationServer.Common.Test;
    using Microsoft.ApplicationServer.Http.Dispatcher;
    using Microsoft.VisualStudio.TestTools.UnitTesting;

    [TestClass, UnitTestLevel(UnitTestLevel.InProgress), UnitTestType(typeof(HttpContentExtensionMethods))]
    public class HttpContentExtensionMethodsTests : UnitTest
    {
        #region Type

        [TestMethod]
        [TestCategory("CIT"), Timeout(TimeoutConstant.DefaultTimeout), Owner("")]
        [Description("HttpContentExtensionMethods is public and static.")]
        public void TypeIsCorrect()
        {
            Type t = this.TypeUnderTest;
            Assert.IsTrue(t.IsPublic, "HttpContentExtensionMethods should be public.");
            Assert.IsTrue(t.IsClass, "HttpContentExtensionMethods should be a class.");
            Assert.IsTrue(t.IsAbstract, "HttpContentExtensionMethods should be abstract.");
            Assert.IsTrue(t.IsSealed, "HttpContentExtensionMethods should be sealed.");

            Assert.AreEqual(0, t.GetConstructors().Length, "HttpContentExtensionMethods should be be static and have no constructors.");
        }

        #endregion Type

        #region Methods

        #region ReadAs(HttpContent, Type)

        [TestMethod]
        [TestCategory("CIT"), Timeout(TimeoutConstant.DefaultTimeout), Owner("")]
        [Description("HttpContentExtensionMethods.ReadAs(HttpContent, Type) throws with null 'this'.")]
        public void ReadAsThrowsWithNullThis()
        {
            StringContent content = null;
            ExceptionAssert.ThrowsArgumentNull("content", () => content.ReadAs(typeof(string)));
        }

        [TestMethod]
        [TestCategory("CIT"), Timeout(TimeoutConstant.DefaultTimeout), Owner("")]
        [Description("HttpContentExtensionMethods.ReadAs(HttpContent, Type) throws with null Type.")]
        public void ReadAsThrowsWithNullType()
        {
            StringContent content = new StringContent(string.Empty);
            ExceptionAssert.ThrowsArgumentNull("type", () => content.ReadAs(null));
        }

        #endregion ReadAs(HttpContent, Type)

        #region ReadAs(HttpContent, Type, IEnumerable<MediaTypeFormatter>)

        [TestMethod]
        [TestCategory("CIT"), Timeout(TimeoutConstant.DefaultTimeout), Owner("")]
        [Description("HttpContentExtensionMethods.ReadAs(HttpContent, Type, IEnumerable<MediaTypeFormatter>) throws with null 'this'.")]
        public void ReadAs1ThrowsWithNullThis()
        {
            StringContent content = null;
            MediaTypeFormatter[] formatters = new MediaTypeFormatter[0];
            ExceptionAssert.ThrowsArgumentNull("content", () => content.ReadAs(typeof(string), formatters));
        }

        [TestMethod]
        [TestCategory("CIT"), Timeout(TimeoutConstant.DefaultTimeout), Owner("")]
        [Description("HttpContentExtensionMethods.ReadAs(HttpContent, Type, IEnumerable<MediaTypeFormatter>) throws with null Type.")]
        public void ReadAs1ThrowsWithNullType()
        {
            StringContent content = new StringContent(string.Empty);
            MediaTypeFormatter[] formatters = new MediaTypeFormatter[0];
            ExceptionAssert.ThrowsArgumentNull("type", () => content.ReadAs(null, formatters));
        }

        [TestMethod]
        [TestCategory("CIT"), Timeout(TimeoutConstant.DefaultTimeout), Owner("")]
        [Description("HttpContentExtensionMethods.ReadAs(HttpContent, Type, IEnumerable<MediaTypeFormatter>) throws with null Formatters.")]
        public void ReadAs1ThrowsWithNullFormatters()
        {
            StringContent content = new StringContent(string.Empty);
            MediaTypeFormatter[] formatters = null;
            ExceptionAssert.ThrowsArgumentNull("formatters", () => content.ReadAs(typeof(string), formatters));
        }

        #endregion ReadAs(HttpContent, Type, IEnumerable<MediaTypeFormatter>)

        #region ReadAsOrDefault(HttpContent, Type)

        [TestMethod]
        [TestCategory("CIT"), Timeout(TimeoutConstant.DefaultTimeout), Owner("")]
        [Description("HttpContentExtensionMethods.ReadAsOrDefault(HttpContent, Type) throws with null 'this'.")]
        public void ReadAsOrDefaultThrowsWithNullThis()
        {
            StringContent content = null;
            ExceptionAssert.ThrowsArgumentNull("content", () => content.ReadAsOrDefault(typeof(string)));
        }

        [TestMethod]
        [TestCategory("CIT"), Timeout(TimeoutConstant.DefaultTimeout), Owner("")]
        [Description("HttpContentExtensionMethods.ReadAsOrDefault(HttpContent, Type) throws with null Type.")]
        public void ReadAsOrDefaultThrowsWithNullType()
        {
            StringContent content = new StringContent(string.Empty);
            ExceptionAssert.ThrowsArgumentNull("type", () => content.ReadAsOrDefault(null));
        }

        #endregion ReadAsOrDefault(HttpContent, Type)

        #region ReadAsOrDefault(HttpContent, Type, IEnumerable<MediaTypeFormatter>)

        [TestMethod]
        [TestCategory("CIT"), Timeout(TimeoutConstant.DefaultTimeout), Owner("")]
        [Description("HttpContentExtensionMethods.ReadAsOrDefault(HttpContent, Type, IEnumerable<MediaTypeFormatter>) throws with null 'this'.")]
        public void ReadAsOrDefault1ThrowsWithNullThis()
        {
            StringContent content = null;
            MediaTypeFormatter[] formatters = new MediaTypeFormatter[0];
            ExceptionAssert.ThrowsArgumentNull("content", () => content.ReadAsOrDefault(typeof(string), formatters));
        }

        [TestMethod]
        [TestCategory("CIT"), Timeout(TimeoutConstant.DefaultTimeout), Owner("")]
        [Description("HttpContentExtensionMethods.ReadAsOrDefault(HttpContent, Type, IEnumerable<MediaTypeFormatter>) throws with null Type.")]
        public void ReadAsOrDefault1ThrowsWithNullType()
        {
            StringContent content = new StringContent(string.Empty);
            MediaTypeFormatter[] formatters = new MediaTypeFormatter[0];
            ExceptionAssert.ThrowsArgumentNull("type", () => content.ReadAsOrDefault(null, formatters));
        }

        [TestMethod]
        [TestCategory("CIT"), Timeout(TimeoutConstant.DefaultTimeout), Owner("")]
        [Description("HttpContentExtensionMethods.ReadAsOrDefault(HttpContent, Type, IEnumerable<MediaTypeFormatter>) throws with null Formatters.")]
        public void ReadAsOrDefault1ThrowsWithNullFormatters()
        {
            StringContent content = new StringContent(string.Empty);
            MediaTypeFormatter[] formatters = null;
            ExceptionAssert.ThrowsArgumentNull("formatters", () => content.ReadAsOrDefault(typeof(string), formatters));
        }

        #endregion ReadAsOrDefault(HttpContent, Type, IEnumerable<MediaTypeFormatter>)

        #region ReadAsAsync(HttpContent, Type)

        [TestMethod]
        [TestCategory("CIT"), Timeout(TimeoutConstant.DefaultTimeout), Owner("")]
        [Description("HttpContentExtensionMethods.ReadAsAsync(HttpContent, Type) throws with null 'this'.")]
        public void ReadAsAsyncThrowsWithNullThis()
        {
            StringContent content = null;
            ExceptionAssert.ThrowsArgumentNull("content", () => content.ReadAsAsync(typeof(string)));
        }

        [TestMethod]
        [TestCategory("CIT"), Timeout(TimeoutConstant.DefaultTimeout), Owner("")]
        [Description("HttpContentExtensionMethods.ReadAsAsync(HttpContent, Type) throws with null Type.")]
        public void ReadAsAsyncThrowsWithNullType()
        {
            StringContent content = new StringContent(string.Empty);
            ExceptionAssert.ThrowsArgumentNull("type", () => content.ReadAsAsync(null));
        }

        #endregion ReadAsAsync(HttpContent, Type)

        #region ReadAsAsync(HttpContent, Type, IEnumerable<MediaTypeFormatter>)

        [TestMethod]
        [TestCategory("CIT"), Timeout(TimeoutConstant.DefaultTimeout), Owner("")]
        [Description("HttpContentExtensionMethods.ReadAsAsync(HttpContent, Type, IEnumerable<MediaTypeFormatter>) throws with null 'this'.")]
        public void ReadAsAsync1ThrowsWithNullThis()
        {
            StringContent content = null;
            MediaTypeFormatter[] formatters = new MediaTypeFormatter[0];
            ExceptionAssert.ThrowsArgumentNull("content", () => content.ReadAsAsync(typeof(string), formatters));
        }

        [TestMethod]
        [TestCategory("CIT"), Timeout(TimeoutConstant.DefaultTimeout), Owner("")]
        [Description("HttpContentExtensionMethods.ReadAsAsync(HttpContent, Type, IEnumerable<MediaTypeFormatter>) throws with null Type.")]
        public void ReadAsAsync1ThrowsWithNullType()
        {
            StringContent content = new StringContent(string.Empty);
            MediaTypeFormatter[] formatters = new MediaTypeFormatter[0];
            ExceptionAssert.ThrowsArgumentNull("type", () => content.ReadAsAsync(null, formatters));
        }

        [TestMethod]
        [TestCategory("CIT"), Timeout(TimeoutConstant.DefaultTimeout), Owner("")]
        [Description("HttpContentExtensionMethods.ReadAsAsync(HttpContent, Type, IEnumerable<MediaTypeFormatter>) throws with null Formatters.")]
        public void ReadAsAsync1ThrowsWithNullFormatters()
        {
            StringContent content = new StringContent(string.Empty);
            MediaTypeFormatter[] formatters = null;
            ExceptionAssert.ThrowsArgumentNull("formatters", () => content.ReadAsAsync(typeof(string), formatters));
        }

        #endregion ReadAsAsync(HttpContent, Type, IEnumerable<MediaTypeFormatter>)

        #region ReadAsOrDefaultAsync(HttpContent, Type)

        [TestMethod]
        [TestCategory("CIT"), Timeout(TimeoutConstant.DefaultTimeout), Owner("")]
        [Description("HttpContentExtensionMethods.ReadAsOrDefaultAsync(HttpContent, Type) throws with null 'this'.")]
        public void ReadAsOrDefaultAsyncThrowsWithNullThis()
        {
            StringContent content = null;
            ExceptionAssert.ThrowsArgumentNull("content", () => content.ReadAsOrDefaultAsync(typeof(string)));
        }

        [TestMethod]
        [TestCategory("CIT"), Timeout(TimeoutConstant.DefaultTimeout), Owner("")]
        [Description("HttpContentExtensionMethods.ReadAsOrDefaultAsync(HttpContent, Type) throws with null Type.")]
        public void ReadAsOrDefaultAsyncThrowsWithNullType()
        {
            StringContent content = new StringContent(string.Empty);
            ExceptionAssert.ThrowsArgumentNull("type", () => content.ReadAsOrDefaultAsync(null));
        }

        #endregion ReadAsOrDefaultAsync(HttpContent, Type)

        #region ReadAsOrDefaultAsync(HttpContent, Type, IEnumerable<MediaTypeFormatter>)

        [TestMethod]
        [TestCategory("CIT"), Timeout(TimeoutConstant.DefaultTimeout), Owner("")]
        [Description("HttpContentExtensionMethods.ReadAsOrDefaultAsync(HttpContent, Type, IEnumerable<MediaTypeFormatter>) throws with null 'this'.")]
        public void ReadAsOrDefaultAsync1ThrowsWithNullThis()
        {
            StringContent content = null;
            MediaTypeFormatter[] formatters = new MediaTypeFormatter[0];
            ExceptionAssert.ThrowsArgumentNull("content", () => content.ReadAsOrDefaultAsync(typeof(string), formatters));
        }

        [TestMethod]
        [TestCategory("CIT"), Timeout(TimeoutConstant.DefaultTimeout), Owner("")]
        [Description("HttpContentExtensionMethods.ReadAsOrDefaultAsync(HttpContent, Type, IEnumerable<MediaTypeFormatter>) throws with null Type.")]
        public void ReadAsOrDefaultAsync1ThrowsWithNullType()
        {
            StringContent content = new StringContent(string.Empty);
            MediaTypeFormatter[] formatters = new MediaTypeFormatter[0];
            ExceptionAssert.ThrowsArgumentNull("type", () => content.ReadAsOrDefaultAsync(null, formatters));
        }

        [TestMethod]
        [TestCategory("CIT"), Timeout(TimeoutConstant.DefaultTimeout), Owner("")]
        [Description("HttpContentExtensionMethods.ReadAsOrDefaultAsync(HttpContent, Type, IEnumerable<MediaTypeFormatter>) throws with null Formatters.")]
        public void ReadAsOrDefaultAsync1ThrowsWithNullFormatters()
        {
            StringContent content = new StringContent(string.Empty);
            MediaTypeFormatter[] formatters = null;
            ExceptionAssert.ThrowsArgumentNull("formatters", () => content.ReadAsOrDefaultAsync(typeof(string), formatters));
        }

        #endregion ReadAsOrDefaultAsync(HttpContent, Type, IEnumerable<MediaTypeFormatter>)

        #endregion Methods
    }
}
