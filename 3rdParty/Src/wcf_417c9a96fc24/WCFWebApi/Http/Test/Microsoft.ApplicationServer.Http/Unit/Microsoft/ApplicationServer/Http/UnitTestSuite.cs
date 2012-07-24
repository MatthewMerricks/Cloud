// <copyright>
//   Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>

namespace Microsoft.ApplicationServer.Http
{
    using System;
    using System.IO;
    using System.Linq;
    using System.Net.Http;
    using System.Net.Http.Headers;
    using Microsoft.ApplicationServer.Http.Dispatcher;
    using Microsoft.ApplicationServer.Http.Moles;
    using System.Threading.Tasks;
    using Microsoft.VisualStudio.TestTools.UnitTesting;
    using Microsoft.ApplicationServer.Common.Test;
    using System.Collections.Generic;
    using System.Collections.ObjectModel;

    /// <summary>
    /// Base class for testing that all product types in the entire
    /// assembly have correct unit tests.
    /// </summary>
    [TestClass, UnitTestLevel(UnitTestLevel.InProgress)]
    public class UnitTestSuite : Microsoft.ApplicationServer.Common.Test.Framework.UnitTestSuite
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="UnitTestSuite"/> class.
        /// </summary>
        public UnitTestSuite()
            : base(typeof(HttpBinding).Assembly)
        {
        }

        /// <summary>
        /// Called to validate all the tests in the assembly.
        /// </summary>
        [TestMethod]
        public override void UnitTestSuiteIsCorrect()
        {
            this.ValidateUnitTestSuite();
        }
    }
}
