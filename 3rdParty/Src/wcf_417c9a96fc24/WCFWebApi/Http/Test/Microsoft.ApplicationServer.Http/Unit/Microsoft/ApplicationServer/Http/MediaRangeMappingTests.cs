// <copyright>
//   Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>

namespace Microsoft.ApplicationServer.Http
{
    using System;
    using System.Net.Http;
    using System.Net.Http.Headers;
    using Microsoft.ApplicationServer.Common.Test;
    using Microsoft.ApplicationServer.Http.Test;
    using Microsoft.VisualStudio.TestTools.UnitTesting;

    [TestClass, UnitTestLevel(UnitTestLevel.InProgress)]
    public class MediaRangeMappingTests : UnitTest<MediaRangeMapping>
    {
        #region Type

        [TestMethod]
        [TestCategory("CIT"), Timeout(TimeoutConstant.DefaultTimeout), Owner("")]
        [Description("MediaRangeMapping is public, concrete, and sealed.")]
        public void TypeIsCorrect()
        {
            Type t = this.TypeUnderTest;
            Assert.IsTrue(t.IsPublic, "MediaRangeMapping should be public.");
            Assert.IsFalse(t.IsAbstract, "MediaRangeMapping should not be abstract.");
            Assert.IsTrue(t.IsSealed, "MediaRangeMapping should be sealed.");
            Assert.AreEqual(typeof(MediaTypeMapping), t.BaseType, "MediaRangeMapping should derive from MediaTypeMapping.");
        }

        #endregion Type

        #region Constructors

        #region MediaRangeMapping(MediaTypeHeaderValue, MediaTypeHeaderValue)

        [TestMethod]
        [TestCategory("CIT"), Timeout(TimeoutConstant.DefaultTimeout), Owner("")]
        [Description("MediaRangeMapping(MediaTypeHeaderValue, MediaTypeHeaderValue) sets public properties.")]
        public void Constructor()
        {
            foreach (MediaTypeHeaderValue mediaRange in HttpTestData.LegalMediaRangeValues)
            {
                foreach (MediaTypeHeaderValue mediaType in HttpTestData.LegalMediaTypeHeaderValues)
                {
                    MediaRangeMapping mapping = new MediaRangeMapping(mediaRange, mediaType);
                    MediaTypeAssert.AreEqual(mediaRange, mapping.MediaRange, "MediaRange failed to set.");
                    MediaTypeAssert.AreEqual(mediaType, mapping.MediaType, "MediaType failed to set.");
                }
            }
        }

        [TestMethod]
        [TestCategory("CIT"), Timeout(TimeoutConstant.DefaultTimeout), Owner("")]
        [Description("MediaRangeMapping(MediaTypeHeaderValue, MediaTypeHeaderValue) throws if the MediaRange parameter is null.")]
        public void ConstructorThrowsWithNullMediaRange()
        {
            foreach (MediaTypeHeaderValue mediaType in HttpTestData.LegalMediaTypeHeaderValues)
            {
                ExceptionAssert.ThrowsArgumentNull("mediaRange", () => new MediaRangeMapping((MediaTypeHeaderValue)null, mediaType));
            }
        }

        [TestMethod]
        [TestCategory("CIT"), Timeout(TimeoutConstant.DefaultTimeout), Owner("")]
        [Description("MediaRangeMapping(MediaTypeHeaderValue, MediaTypeHeaderValue) throws if the MediaType parameter is null.")]
        public void ConstructorThrowsWithNullMediaType()
        {
            foreach (MediaTypeHeaderValue mediaRange in HttpTestData.LegalMediaRangeValues)
            {
                ExceptionAssert.ThrowsArgumentNull("mediaType", () => new MediaRangeMapping(mediaRange, (MediaTypeHeaderValue)null));
            }
        }

        [TestMethod]
        [TestCategory("CIT"), Timeout(TimeoutConstant.DefaultTimeout), Owner("")]
        [Description("MediaRangeMapping(MediaTypeHeaderValue, MediaTypeHeaderValue) throws if the MediaRange parameter is not really a media range.")]
        public void ConstructorThrowsWithIllegalMediaRange()
        {
            foreach (MediaTypeHeaderValue mediaRange in HttpTestData.IllegalMediaRangeValues)
            {
                foreach (MediaTypeHeaderValue mediaType in HttpTestData.LegalMediaTypeHeaderValues)
                {
                    string errorMessage = SR.InvalidMediaRange(mediaRange.MediaType);
                    ExceptionAssert.Throws<InvalidOperationException>("Invalid media range should throw.", errorMessage, () => new MediaRangeMapping(mediaRange, mediaType));
                }
            }
        }

        #endregion MediaRangeMapping(MediaTypeHeaderValue, MediaTypeHeaderValue)


        #region MediaRangeMapping(string, string)

        [TestMethod]
        [TestCategory("CIT"), Timeout(TimeoutConstant.DefaultTimeout), Owner("")]
        [Description("MediaRangeMapping(string, string) sets public properties.")]
        public void Constructor1()
        {
            foreach (string mediaRange in HttpTestData.LegalMediaRangeStrings)
            {
                foreach (string mediaType in HttpTestData.LegalMediaTypeStrings)
                {
                    MediaRangeMapping mapping = new MediaRangeMapping(mediaRange, mediaType);
                    MediaTypeAssert.AreEqual(mediaRange, mapping.MediaRange, "MediaRange failed to set.");
                    MediaTypeAssert.AreEqual(mediaType, mapping.MediaType, "MediaType failed to set.");
                }
            }
        }

        [TestMethod]
        [TestCategory("CIT"), Timeout(TimeoutConstant.DefaultTimeout), Owner("")]
        [Description("MediaRangeMapping(string, stringe) throws if the MediaRange parameter is empty.")]
        public void Constructor1ThrowsWithEmptyMediaRange()
        {
            foreach (string mediaRange in TestData.EmptyStrings)
            {
                foreach (string mediaType in HttpTestData.LegalMediaTypeStrings)
                {
                    ExceptionAssert.ThrowsArgumentNull("mediaRange", () => new MediaRangeMapping(mediaRange, mediaType));
                }
            }
        }

        [TestMethod]
        [TestCategory("CIT"), Timeout(TimeoutConstant.DefaultTimeout), Owner("")]
        [Description("MediaRangeMapping(Mstring, string) throws if the MediaType parameter is empty.")]
        public void Constructor1ThrowsWithEmptyMediaType()
        {
            foreach (string mediaRange in HttpTestData.LegalMediaRangeStrings)
            {
                foreach (string mediaType in TestData.EmptyStrings)
                {
                    ExceptionAssert.ThrowsArgumentNull("mediaType", () => new MediaRangeMapping(mediaRange, mediaType));
                }
            }
        }

        [TestMethod]
        [TestCategory("CIT"), Timeout(TimeoutConstant.DefaultTimeout), Owner("")]
        [Description("MediaRangeMapping(string, string) throws if the MediaRange parameter is not really a media range.")]
        public void Constructor1ThrowsWithIllegalMediaRange()
        {
            foreach (string mediaRange in HttpTestData.IllegalMediaRangeStrings)
            {
                foreach (string mediaType in HttpTestData.LegalMediaTypeStrings)
                {
                    string errorMessage = SR.InvalidMediaRange(mediaRange);
                    ExceptionAssert.Throws<InvalidOperationException>("Invalid media range should throw.", errorMessage, () => new MediaRangeMapping(mediaRange, mediaType));
                }
            }
        }

        #endregion MediaRangeMapping(string, string)

        #endregion  Constructors

        #region Properties

        #endregion Properties

        #region Methods

        #region SupportsMediaType(HttpRequestMessage)

        [TestMethod]
        [TestCategory("CIT"), Timeout(TimeoutConstant.DefaultTimeout), Owner("")]
        [Description("SupportsMediaType(HttpRequestMessage) returns false unconditionally.")]
        public void SupportsMediaTypeReturnsFalseAlways()
        {
            foreach (string mediaRange in HttpTestData.LegalMediaRangeStrings)
            {
                foreach (string mediaType in HttpTestData.LegalMediaTypeStrings)
                {
                    MediaRangeMapping mapping = new MediaRangeMapping(mediaRange, mediaType);
                    HttpRequestMessage request = new HttpRequestMessage();
                    Assert.IsFalse(mapping.SupportsMediaType(request), "SupportsMediaType should have returned false.");
                }
            }
        }

        [TestMethod]
        [TestCategory("CIT"), Timeout(TimeoutConstant.DefaultTimeout), Owner("")]
        [Description("SupportsMediaType(HttpRequestMessage) throws with null HttpRequestMessage.")]
        public void SupportsMediaTypeThrowsWithNullHttpRequestMessage()
        {
            foreach (string mediaRange in HttpTestData.LegalMediaRangeStrings)
            {
                foreach (string mediaType in HttpTestData.LegalMediaTypeStrings)
                {
                    MediaRangeMapping mapping = new MediaRangeMapping(mediaRange, mediaType);
                    HttpRequestMessage request = null;
                    ExceptionAssert.ThrowsArgumentNull(
                        "request",
                        () => mapping.SupportsMediaType(request));
                }
            }
        }

        #endregion SupportsMediaType(HttpRequestMessage)

        #region SupportsMediaType(HttpResponseMessage)

        [TestMethod]
        [TestCategory("CIT"), Timeout(TimeoutConstant.DefaultTimeout), Owner("")]
        [Description("SupportsMediaType(HttpResponseMessage) returns true when the MediaRange is in the accept headers.")]
        public void SupportsMediaType1ReturnsTrueWithMediaRangeInAcceptHeader()
        {
            foreach (string mediaRange in HttpTestData.LegalMediaRangeStrings)
            {
                foreach (string mediaType in HttpTestData.LegalMediaTypeStrings)
                {
                    MediaRangeMapping mapping = new MediaRangeMapping(mediaRange, mediaType);
                    HttpRequestMessage request = new HttpRequestMessage();
                    request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue(mediaRange));
                    HttpResponseMessage response = new HttpResponseMessage() { RequestMessage = request };
                    Assert.IsTrue(mapping.SupportsMediaType(response), string.Format("SupportsMediaType should have returned true for '{0}'.", mediaRange));
                }
            }
        }

        [TestMethod]
        [TestCategory("CIT"), Timeout(TimeoutConstant.DefaultTimeout), Owner("")]
        [Description("SupportsMediaType(HttpResponseMessage) returns false when the MediaRange is not in the accept headers.")]
        public void SupportsMediaType1ReturnsFalseWithMediaRangeNotInAcceptHeader()
        {
            foreach (string mediaRange in HttpTestData.LegalMediaRangeStrings)
            {
                foreach (string mediaType in HttpTestData.LegalMediaTypeStrings)
                {
                    MediaRangeMapping mapping = new MediaRangeMapping(mediaRange, mediaType);
                    HttpRequestMessage request = new HttpRequestMessage();
                    HttpResponseMessage response = new HttpResponseMessage() { RequestMessage = request };
                    Assert.IsFalse(mapping.SupportsMediaType(response), "SupportsMediaType should have returned false for empty accept headers");
                }
            }
        }

        [TestMethod]
        [TestCategory("CIT"), Timeout(TimeoutConstant.DefaultTimeout), Owner("")]
        [Description("SupportsMediaType(HttpResponseMessage) throws with null HttpResponseMessage.")]
        public void SupportsMediaType1ThrowsWithNullHttpResponseMessage()
        {
            foreach (string mediaRange in HttpTestData.LegalMediaRangeStrings)
            {
                foreach (string mediaType in HttpTestData.LegalMediaTypeStrings)
                {
                    MediaRangeMapping mapping = new MediaRangeMapping(mediaRange, mediaType);
                    HttpResponseMessage response = null;
                    ExceptionAssert.ThrowsArgumentNull(
                        "response",
                        () => mapping.SupportsMediaType(response));
                }
            }
        }

        [TestMethod]
        [TestCategory("CIT"), Timeout(TimeoutConstant.DefaultTimeout), Owner("")]
        [Description("SupportsMediaType(HttpResponseMessage) throws with a null HttpRequestMessage in HttpResponseMessage.")]
        public void SupportsMediaType1ThrowsWithNullRequestInHttpResponseMessage()
        {
            foreach (string mediaRange in HttpTestData.LegalMediaRangeStrings)
            {
                foreach (string mediaType in HttpTestData.LegalMediaTypeStrings)
                {
                    MediaRangeMapping mapping = new MediaRangeMapping(mediaRange, mediaType);
                    string errorMessage = SR.ResponseMustReferenceRequest(typeof(HttpResponseMessage).Name, "response", typeof(HttpRequestMessage).Name, "RequestMessage");
                    ExceptionAssert.Throws<InvalidOperationException>("Null request should throw", errorMessage, () => mapping.SupportsMediaType(new HttpResponseMessage()));
                }
            }
        }

        #endregion SupportsMediaType(HttpResponseMessage)

        #endregion Methods

    }
}
