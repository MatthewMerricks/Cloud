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

    [TestClass]
    public class MediaTypeHeadeValueEqualityComparerTests
    {
        #region EqualityComparer Tests

        [TestMethod]
        [TestCategory("CIT")]
        [Timeout(TimeoutConstant.DefaultTimeout)]
        [Owner("")]
        [Description("MediaTypeHeadeValueEqualityComparer.EqualityComparer returns same MediaTypeHeadeValueEqualityComparer instance each time.")]
        public void EqualityComparer_Returns_MediaTypeHeadeValueEqualityComparer()
        {
            MediaTypeHeaderValueEqualityComparer comparer1 = MediaTypeHeaderValueEqualityComparer.EqualityComparer;
            MediaTypeHeaderValueEqualityComparer comparer2 = MediaTypeHeaderValueEqualityComparer.EqualityComparer;

            Assert.IsNotNull(comparer1, "MediaTypeHeaderValueEqualityComparer.EqualityComparer should not have returned null.");
            Assert.AreSame(comparer1, comparer2, "MediaTypeHeaderValueEqualityComparer.EqualityComparer should have returned the same instance both times.");
        }

        #endregion EqualityComparer Tests

        #region GetHashCode Tests

        [TestMethod]
        [TestCategory("CIT")]
        [Timeout(TimeoutConstant.DefaultTimeout)]
        [Owner("")]
        [Description("MediaTypeHeaderValueEqualityComparer.GetHashCode returns the same hash code for media types that differe only be case.")]
        public void GetHashCode_Returns_Same_Hash_Code_Regardless_Of_Case()
        {
            MediaTypeHeaderValueEqualityComparer comparer = MediaTypeHeaderValueEqualityComparer.EqualityComparer;

            MediaTypeHeaderValue mediaType1 = new MediaTypeHeaderValue("text/xml");
            MediaTypeHeaderValue mediaType2 = new MediaTypeHeaderValue("TEXT/xml");
            Assert.AreEqual(comparer.GetHashCode(mediaType1), comparer.GetHashCode(mediaType2), "GetHashCode should have returned the same hash codes.");

            mediaType1 = new MediaTypeHeaderValue("text/*");
            mediaType2 = new MediaTypeHeaderValue("TEXT/*");
            Assert.AreEqual(comparer.GetHashCode(mediaType1), comparer.GetHashCode(mediaType2), "GetHashCode should have returned the same hash codes.");

            mediaType1 = new MediaTypeHeaderValue("*/*");
            mediaType2 = new MediaTypeHeaderValue("*/*");
            Assert.AreEqual(comparer.GetHashCode(mediaType1), comparer.GetHashCode(mediaType2), "GetHashCode should have returned the same hash codes.");
        }

        [TestMethod]
        [TestCategory("CIT")]
        [Timeout(TimeoutConstant.DefaultTimeout)]
        [Owner("")]
        [Description("MediaTypeHeaderValueEqualityComparer.GetHashCode returns different hash codes if the media types are different.")]
        public void GetHashCode_Returns_Different_Hash_Code_For_Different_Media_Type()
        {
            MediaTypeHeaderValueEqualityComparer comparer = MediaTypeHeaderValueEqualityComparer.EqualityComparer;

            MediaTypeHeaderValue mediaType1 = new MediaTypeHeaderValue("text/*");
            MediaTypeHeaderValue mediaType2 = new MediaTypeHeaderValue("TEXT/xml");
            Assert.AreNotEqual(comparer.GetHashCode(mediaType1), comparer.GetHashCode(mediaType2), "GetHashCode should have returned different hash codes.");

            mediaType1 = new MediaTypeHeaderValue("application/*");
            mediaType2 = new MediaTypeHeaderValue("TEXT/*");
            Assert.AreNotEqual(comparer.GetHashCode(mediaType1), comparer.GetHashCode(mediaType2), "GetHashCode should have returned different hash codes.");

            mediaType1 = new MediaTypeHeaderValue("application/*");
            mediaType2 = new MediaTypeHeaderValue("*/*");
            Assert.AreNotEqual(comparer.GetHashCode(mediaType1), comparer.GetHashCode(mediaType2), "GetHashCode should have returned different hash codes.");
        }

        #endregion GetHashCode Tests

        #region Equals Tests

        [TestMethod]
        [TestCategory("CIT")]
        [Timeout(TimeoutConstant.DefaultTimeout)]
        [Owner("")]
        [Description("MediaTypeHeaderValueEqualityComparer.Equals returns true if media types and charsets differ only by case.")]
        public void Equals_Returns_True_If_MediaTypes_Differ_Only_By_Case()
        {
            MediaTypeHeaderValueEqualityComparer comparer = MediaTypeHeaderValueEqualityComparer.EqualityComparer;

            MediaTypeHeaderValue mediaType1 = new MediaTypeHeaderValue("text/xml");
            MediaTypeHeaderValue mediaType2 = new MediaTypeHeaderValue("TEXT/xml");
            Assert.IsTrue(comparer.Equals(mediaType1, mediaType2), "Equals should have returned 'true'.");

            mediaType1 = new MediaTypeHeaderValue("text/*");
            mediaType2 = new MediaTypeHeaderValue("TEXT/*");
            Assert.IsTrue(comparer.Equals(mediaType1, mediaType2), "Equals should have returned 'true'.");

            mediaType1 = new MediaTypeHeaderValue("*/*");
            mediaType2 = new MediaTypeHeaderValue("*/*");
            Assert.IsTrue(comparer.Equals(mediaType1, mediaType2), "Equals should have returned 'true'.");

            mediaType1 = new MediaTypeHeaderValue("text/*");
            mediaType1.CharSet = "someCharset";
            mediaType2 = new MediaTypeHeaderValue("TEXT/*");
            mediaType2.CharSet = "SOMECHARSET";
            Assert.IsTrue(comparer.Equals(mediaType1, mediaType2), "Equals should have returned 'true'.");

            mediaType1 = new MediaTypeHeaderValue("application/*");
            mediaType1.CharSet = "";
            mediaType2 = new MediaTypeHeaderValue("application/*");
            mediaType2.CharSet = null;
            Assert.IsTrue(comparer.Equals(mediaType1, mediaType2), "Equals should have returned 'true'.");
        }

        [TestMethod]
        [TestCategory("CIT")]
        [Timeout(TimeoutConstant.DefaultTimeout)]
        [Owner("")]
        [Description("MediaTypeHeaderValueEqualityComparer.Equals returns false if media types and charsets differ by more than case.")]
        public void Equals_Returns_False_If_MediaTypes_Differ_By_More_Than_Case()
        {
            MediaTypeHeaderValueEqualityComparer comparer = MediaTypeHeaderValueEqualityComparer.EqualityComparer;

            MediaTypeHeaderValue mediaType1 = new MediaTypeHeaderValue("text/xml");
            MediaTypeHeaderValue mediaType2 = new MediaTypeHeaderValue("TEST/xml");
            Assert.IsFalse(comparer.Equals(mediaType1, mediaType2), "Equals should have returned 'false'.");

            mediaType1 = new MediaTypeHeaderValue("text/*");
            mediaType1.CharSet = "someCharset";
            mediaType2 = new MediaTypeHeaderValue("TEXT/*");
            mediaType2.CharSet = "SOMEOTHERCHARSET";
            Assert.IsFalse(comparer.Equals(mediaType1, mediaType2), "Equals should have returned 'false'.");
        }

        #endregion GetHashCode Tests
    }
}
