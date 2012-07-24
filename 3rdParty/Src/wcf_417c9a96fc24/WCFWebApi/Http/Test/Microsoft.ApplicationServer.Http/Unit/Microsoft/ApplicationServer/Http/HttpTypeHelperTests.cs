// <copyright>
//   Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
namespace Microsoft.ApplicationServer.Http.Dispatcher
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Net;
    using System.Net.Http;
    using System.ServiceModel.Dispatcher;
    using System.ServiceModel.Web;
    using System.Text;
    using Microsoft.VisualStudio.TestTools.UnitTesting;
    using Microsoft.ApplicationServer.Common.Test;

    [TestClass, UnitTestLevel(UnitTestLevel.InProgress), UnitTestType(typeof(HttpTypeHelper))]
    public class HttpTypeHelperTests : UnitTest
    {
        #region Type

        [TestMethod]
        [TestCategory("CIT"), Timeout(TimeoutConstant.DefaultTimeout), Owner("")]
        [Description("HttpTypeHelper is internal and static.")]
        public void TypeIsCorrect()
        {
            Type t = this.TypeUnderTest;
            Assert.IsFalse(t.IsPublic, "HttpTypeHelper should be internal.");
            Assert.IsTrue(t.IsClass, "HttpTypeHelper should be a class.");
            Assert.IsTrue(t.IsAbstract, "HttpTypeHelper should be abstract.");
            Assert.IsTrue(t.IsSealed, "HttpTypeHelper should be sealed.");

            Assert.AreEqual(0, t.GetConstructors().Length, "MediaTypeFormatterExtensionMethods should be static and have no constructors.");
        }

        #endregion Type

        #region Methods

        #endregion Methods
    }
}
