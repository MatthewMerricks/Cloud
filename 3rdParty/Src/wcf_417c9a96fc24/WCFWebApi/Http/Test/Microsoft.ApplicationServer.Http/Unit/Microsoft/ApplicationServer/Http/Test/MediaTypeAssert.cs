// <copyright>
//   Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>

namespace Microsoft.ApplicationServer.Http.Test
{
    using System;
    using System.Collections.Generic;
    using System.Collections.ObjectModel;
    using System.IO;
    using System.Linq;
    using System.Net.Http;
    using System.Net.Http.Headers;
    using System.Text;
    using Microsoft.ApplicationServer.Common.Test;
    using Microsoft.ApplicationServer.Common.Test.Types;
    using Microsoft.VisualStudio.TestTools.UnitTesting;

    public static class MediaTypeAssert
    {
        public static void AreEqual(MediaTypeHeaderValue expected, MediaTypeHeaderValue actual, string errorMessage)
        {
            if (expected != null || actual != null)
            {
                Assert.IsNotNull(expected, string.Format("{0}  Expected media type should not be null.", errorMessage));
                Assert.IsTrue(MediaTypeHeaderValueComparer.Equals(expected, actual), string.Format("{0}  Expected media type '{1}' but found media type '{2}'.", errorMessage, expected, actual));
            }
        }

        public static void AreEqual(MediaTypeHeaderValue expected, string actual, string errorMessage)
        {
            if (expected != null || !string.IsNullOrEmpty(actual))
            {
                MediaTypeHeaderValue actualMediaType = new MediaTypeHeaderValue(actual);
                Assert.IsNotNull(expected, string.Format("{0}: expected media type should not be null.", errorMessage));
                Assert.IsTrue(MediaTypeHeaderValueComparer.Equals(expected, actualMediaType), string.Format("{0}  Expected media type '{1}' but found media type '{2}'.", errorMessage, expected, actual));
            }
        }

        public static void AreEqual(string expected, string actual, string errorMessage)
        {
            if (!string.IsNullOrEmpty(expected) || !string.IsNullOrEmpty(actual))
            {
                Assert.IsNotNull(expected, string.Format("{0}: expected media type should not be null.", errorMessage));
                MediaTypeHeaderValue expectedMediaType = new MediaTypeHeaderValue(expected);
                MediaTypeHeaderValue actualMediaType = new MediaTypeHeaderValue(actual);
                Assert.IsTrue(MediaTypeHeaderValueComparer.Equals(expectedMediaType, actualMediaType), string.Format("{0}  Expected media type '{1}' but found media type '{2}'.", errorMessage, expected, actual));
            }
        }

        public static void AreEqual(string expected, MediaTypeHeaderValue actual, string errorMessage)
        {
            if (!string.IsNullOrEmpty(expected) || actual != null)
            {
                Assert.IsNotNull(expected, string.Format("{0}: expected media type should not be null.", errorMessage));
                MediaTypeHeaderValue expectedMediaType = new MediaTypeHeaderValue(expected);;
                Assert.IsTrue(MediaTypeHeaderValueComparer.Equals(expectedMediaType, actual), string.Format("{0}  Expected media type '{1}' but found media type '{2}'.", errorMessage, expected, actual));
            }
        }
    }
}
