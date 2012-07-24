// <copyright>
//   Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>

namespace Microsoft.ApplicationServer.Http
{
    using System;
    using System.Linq;
    using System.Net.Http;
    using System.Net.Http.Headers;
    using Microsoft.ApplicationServer.Common.Test;
    using Microsoft.ApplicationServer.Http.Test;
    using Microsoft.VisualStudio.TestTools.UnitTesting;

    [TestClass, UnitTestLevel(UnitTestLevel.InProgress)]
    public class QueryStringMappingTests : UnitTest<QueryStringMapping>
    {
        #region Type

        [TestMethod]
        [TestCategory("CIT"), Timeout(TimeoutConstant.DefaultTimeout), Owner("")]
        [Description("QueryStringMapping is public, concrete, and sealed.")]
        public void TypeIsCorrect()
        {
            Type t = this.TypeUnderTest;
            Assert.IsTrue(t.IsPublic, "QueryStringMapping should be public.");
            Assert.IsFalse(t.IsAbstract, "QueryStringMapping should not be abstract.");
            Assert.IsTrue(t.IsSealed, "QueryStringMapping should be sealed.");
            Assert.AreEqual(typeof(MediaTypeMapping), t.BaseType, "MediaRangeMapping should derive from MediaTypeMapping.");
        }

        #endregion Type

        #region Constructors

        #region Constructor(string, string, MediaTypeHeaderValue)

        [TestMethod]
        [TestCategory("CIT"), Timeout(TimeoutConstant.DefaultTimeout), Owner("")]
        [Description("QueryStringMapping(string, string, MediaTypeHeaderValue) sets properties.")]
        public void Constructor()
        {
            foreach (string queryStringParameterName in HttpTestData.LegalQueryStringParameterNames)
            {
                foreach (string queryStringParameterValue in HttpTestData.LegalQueryStringParameterValues)
                {
                    foreach (MediaTypeHeaderValue mediaType in HttpTestData.LegalMediaTypeHeaderValues)
                    {
                        QueryStringMapping mapping = new QueryStringMapping(queryStringParameterName, queryStringParameterValue, mediaType);
                        Assert.AreEqual(queryStringParameterName, mapping.QueryStringParameterName, "QueryStringParameterName failed to set.");
                        Assert.AreEqual(queryStringParameterValue, mapping.QueryStringParameterValue, "QueryStringParameterValue failed to set.");
                        MediaTypeAssert.AreEqual(mediaType, mapping.MediaType, "MediaType failed to set.");
                    }
                }
            }
        }

        [TestMethod]
        [TestCategory("CIT"), Timeout(TimeoutConstant.DefaultTimeout), Owner("")]
        [Description("QueryStringMapping(string, string, MediaTypeHeaderValue) throws with empty QueryStringParameterName.")]
        public void ConstructorThrowsWithEmptyQueryParameterName()
        {
            foreach (MediaTypeHeaderValue mediaType in HttpTestData.LegalMediaTypeHeaderValues)
            {
                foreach (string queryStringParameterName in TestData.EmptyStrings)
                {
                    ExceptionAssert.ThrowsArgumentNull("queryStringParameterName", () => new QueryStringMapping(queryStringParameterName, "json", mediaType));
                }
            }
        }

        [TestMethod]
        [TestCategory("CIT"), Timeout(TimeoutConstant.DefaultTimeout), Owner("")]
        [Description("QueryStringMapping(string, string, MediaTypeHeaderValue) throws with empty QueryStringParameterValue.")]
        public void ConstructorThrowsWithEmptyQueryParameterValue()
        {
            foreach (MediaTypeHeaderValue mediaType in HttpTestData.LegalMediaTypeHeaderValues)
            {
                foreach (string queryStringParameterValue in TestData.EmptyStrings)
                {
                    ExceptionAssert.ThrowsArgumentNull("queryStringParameterValue", () => new QueryStringMapping("query", queryStringParameterValue, mediaType));
                }
            }
        }

        [TestMethod]
        [TestCategory("CIT"), Timeout(TimeoutConstant.DefaultTimeout), Owner("")]
        [Description("QueryStringMapping(string, string, MediaTypeHeaderValue) throws with null MediaTypeHeaderValue.")]
        public void ConstructorThrowsWithNullMediaTypeHeaderValue()
        {
            foreach (string queryStringParameterName in HttpTestData.LegalQueryStringParameterNames)
            {
                foreach (string queryStringParameterValue in HttpTestData.LegalQueryStringParameterValues)
                {
                    ExceptionAssert.ThrowsArgumentNull("mediaType", () => new QueryStringMapping(queryStringParameterName, queryStringParameterValue, (MediaTypeHeaderValue)null));
                }
            }
        }

        #endregion Constructor(string, string, MediaTypeHeaderValue)

        #region Constructor1(string, string, string)

        [TestMethod]
        [TestCategory("CIT"), Timeout(TimeoutConstant.DefaultTimeout), Owner("")]
        [Description("QueryStringMapping(string, string, string) sets properties.")]
        public void Constructor1()
        {
            foreach (string queryStringParameterName in HttpTestData.LegalQueryStringParameterNames)
            {
                foreach (string queryStringParameterValue in HttpTestData.LegalQueryStringParameterValues)
                {
                    foreach (string mediaType in HttpTestData.LegalMediaTypeStrings)
                    {
                        QueryStringMapping mapping = new QueryStringMapping(queryStringParameterName, queryStringParameterValue, mediaType);
                        Assert.AreEqual(queryStringParameterName, mapping.QueryStringParameterName, "QueryStringParameterName failed to set.");
                        Assert.AreEqual(queryStringParameterValue, mapping.QueryStringParameterValue, "QueryStringParameterValue failed to set.");
                        MediaTypeAssert.AreEqual(mediaType, mapping.MediaType, "MediaType failed to set.");
                    }
                }
            }
        }

        [TestMethod]
        [TestCategory("CIT"), Timeout(TimeoutConstant.DefaultTimeout), Owner("")]
        [Description("QueryStringMapping(string, string, string) throws with empty QueryStringParameterName.")]
        public void Constructor1ThrowsWithEmptyQueryParameterName()
        {
            foreach (string mediaType in HttpTestData.LegalMediaTypeStrings)
            {
                foreach (string queryStringParameterName in TestData.EmptyStrings)
                {
                    ExceptionAssert.ThrowsArgumentNull("queryStringParameterName", () => new QueryStringMapping(queryStringParameterName, "json", mediaType));
                }
            }
        }

        [TestMethod]
        [TestCategory("CIT"), Timeout(TimeoutConstant.DefaultTimeout), Owner("")]
        [Description("QueryStringMapping(string, string, string) throws with empty QueryStringParameterValue.")]
        public void Constructor1ThrowsWithEmptyQueryParameterValue()
        {
            foreach (string mediaType in HttpTestData.LegalMediaTypeStrings)
            {
                foreach (string queryStringParameterValue in TestData.EmptyStrings)
                {
                    ExceptionAssert.ThrowsArgumentNull("queryStringParameterValue", () => new QueryStringMapping("query", queryStringParameterValue, mediaType));
                }
            }
        }

        [TestMethod]
        [TestCategory("CIT"), Timeout(TimeoutConstant.DefaultTimeout), Owner("")]
        [Description("QueryStringMapping(string, string, string) throws with empty MediaType.")]
        public void Constructor1ThrowsWithEmptyMediaType()
        {
            foreach (string queryStringParameterName in HttpTestData.LegalQueryStringParameterNames)
            {
                foreach (string queryStringParameterValue in HttpTestData.LegalQueryStringParameterValues)
                {
                    foreach (string mediaType in TestData.EmptyStrings)
                    {
                        ExceptionAssert.ThrowsArgumentNull("mediaType", () => new QueryStringMapping(queryStringParameterName, queryStringParameterValue, (MediaTypeHeaderValue)null));
                    }
                }
            }
        }

        #endregion Constructor(string, string, string)

        #endregion  Constructors

        #region Properties

        #endregion Properties

        #region Methods

        #region SupportsMediaType(HttpRequestMessage)

        [TestMethod]
        [TestCategory("CIT"), Timeout(TimeoutConstant.DefaultTimeout), Owner("")]
        [Description("SupportsMediaType(HttpRequestMessage) returns true when the QueryStringParameterName and QueryStringParameterValue are in the Uri.")]
        public void SupportsMediaTypeReturnsTrueWithQueryStringParameterNameAndValueInUri()
        {
            foreach (string queryStringParameterName in HttpTestData.LegalQueryStringParameterNames)
            {
                foreach (string queryStringParameterValue in HttpTestData.LegalQueryStringParameterValues)
                {
                    foreach (string mediaType in HttpTestData.LegalMediaTypeStrings)
                    {
                        QueryStringMapping mapping = new QueryStringMapping(queryStringParameterName, queryStringParameterValue, mediaType);
                        foreach (string uriBase in TestData.UriTestDataStrings.Where((s) => !s.Contains('?')))
                        {
                            string uri = uriBase + "?" + queryStringParameterName + "=" + queryStringParameterValue;
                            HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Get, uri);
                            Assert.IsTrue(mapping.SupportsMediaType(request), string.Format("SupportsMediaType should have returned true for '{0}'.", uri));
                        }
                    }
                }
            }
        }

        [TestMethod]
        [TestCategory("CIT"), Timeout(TimeoutConstant.DefaultTimeout), Owner("")]
        [Description("SupportsMediaType(HttpRequestMessage) returns false when the QueryStringParameterName is not in the Uri.")]
        public void SupportsMediaTypeReturnsFalseWithQueryStringParameterNameNotInUri()
        {
            foreach (string queryStringParameterName in HttpTestData.LegalQueryStringParameterNames)
            {
                foreach (string queryStringParameterValue in HttpTestData.LegalQueryStringParameterValues)
                {
                    foreach (string mediaType in HttpTestData.LegalMediaTypeStrings)
                    {
                        QueryStringMapping mapping = new QueryStringMapping(queryStringParameterName, queryStringParameterValue, mediaType);
                        foreach (string uriBase in TestData.UriTestDataStrings.Where((s) => !s.Contains('?')))
                        {
                            string uri = uriBase + "?" + "not" + queryStringParameterName + "=" + queryStringParameterValue;
                            HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Get, uri);
                            Assert.IsFalse(mapping.SupportsMediaType(request), string.Format("SupportsMediaType should have returned true for '{0}'.", uri));
                        }
                    }
                }
            }
        }

        [TestMethod]
        [TestCategory("CIT"), Timeout(TimeoutConstant.DefaultTimeout), Owner("")]
        [Description("SupportsMediaType(HttpRequestMessage) returns false when the QueryStringParameterValue is not in the Uri.")]
        public void SupportsMediaTypeReturnsFalseWithQueryStringParameterValueNotInUri()
        {
            foreach (string queryStringParameterName in HttpTestData.LegalQueryStringParameterNames)
            {
                foreach (string queryStringParameterValue in HttpTestData.LegalQueryStringParameterValues)
                {
                    foreach (string mediaType in HttpTestData.LegalMediaTypeStrings)
                    {
                        QueryStringMapping mapping = new QueryStringMapping(queryStringParameterName, queryStringParameterValue, mediaType);
                        foreach (string uriBase in TestData.UriTestDataStrings.Where((s) => !s.Contains('?')))
                        {
                            string uri = uriBase + "?" + queryStringParameterName + "=" + "not" + queryStringParameterValue;
                            HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Get, uri);
                            Assert.IsFalse(mapping.SupportsMediaType(request), string.Format("SupportsMediaType should have returned true for '{0}'.", uri));
                        }
                    }
                }
            }
        }

        [TestMethod]
        [TestCategory("CIT"), Timeout(TimeoutConstant.DefaultTimeout), Owner("")]
        [Description("SupportsMediaType(HttpRequestMessage) throws with a null HttpRequestMessage.")]
        public void SupportsMediaTypeThrowsWithNullHttpRequestMessage()
        {
            foreach (string queryStringParameterName in HttpTestData.LegalQueryStringParameterNames)
            {
                foreach (string queryStringParameterValue in HttpTestData.LegalQueryStringParameterValues)
                {
                    foreach (string mediaType in HttpTestData.LegalMediaTypeStrings)
                    {
                        QueryStringMapping mapping = new QueryStringMapping(queryStringParameterName, queryStringParameterValue, mediaType);
                        ExceptionAssert.ThrowsArgumentNull("request", () => mapping.SupportsMediaType((HttpRequestMessage)null));
                    }
                }
            }
        }

        [TestMethod]
        [TestCategory("CIT"), Timeout(TimeoutConstant.DefaultTimeout), Owner("")]
        [Description("SupportsMediaType(HttpRequestMessage) throws with a null Uri in HttpRequestMessage.")]
        public void SupportsMediaTypeThrowsWithNullUriInHttpRequestMessage()
        {
            foreach (string queryStringParameterName in HttpTestData.LegalQueryStringParameterNames)
            {
                foreach (string queryStringParameterValue in HttpTestData.LegalQueryStringParameterValues)
                {
                    foreach (string mediaType in HttpTestData.LegalMediaTypeStrings)
                    {
                        QueryStringMapping mapping = new QueryStringMapping(queryStringParameterName, queryStringParameterValue, mediaType);
                        string errorMessage = SR.NonNullUriRequiredForMediaTypeMapping(this.TypeUnderTest.Name);
                        ExceptionAssert.Throws<InvalidOperationException>("Null Uri should throw.", errorMessage, () => mapping.SupportsMediaType(new HttpRequestMessage()));
                    }
                }
            }
        }

        #endregion SupportsMediaType(HttpRequestMessage)

        #region SupportsMediaType(HttpResponseMessage)


        [TestMethod]
        [TestCategory("CIT"), Timeout(TimeoutConstant.DefaultTimeout), Owner("")]
        [Description("SupportsMediaType(HttpResponseMessage) returns true when the QueryStringParameterName and QueryStringParameterValue are in the Uri.")]
        public void SupportsMediaType1ReturnsTrueWithQueryStringParameterNameAndValueInUri()
        {
            foreach (string queryStringParameterName in HttpTestData.LegalQueryStringParameterNames)
            {
                foreach (string queryStringParameterValue in HttpTestData.LegalQueryStringParameterValues)
                {
                    foreach (string mediaType in HttpTestData.LegalMediaTypeStrings)
                    {
                        QueryStringMapping mapping = new QueryStringMapping(queryStringParameterName, queryStringParameterValue, mediaType);
                        foreach (string uriBase in TestData.UriTestDataStrings.Where((s) => !s.Contains('?')))
                        {
                            string uri = uriBase + "?" + queryStringParameterName + "=" + queryStringParameterValue;
                            HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Get, uri);
                            HttpResponseMessage response = new HttpResponseMessage() { RequestMessage = request };
                            Assert.IsTrue(mapping.SupportsMediaType(response), string.Format("SupportsMediaType should have returned true for '{0}'.", uri));
                        }
                    }
                }
            }
        }

        [TestMethod]
        [TestCategory("CIT"), Timeout(TimeoutConstant.DefaultTimeout), Owner("")]
        [Description("SupportsMediaType(HttpResponseMessage) returns false when the QueryStringParameterName is not in the Uri.")]
        public void SupportsMediaType1ReturnsFalseWithQueryStringParameterNameNotInUri()
        {
            foreach (string queryStringParameterName in HttpTestData.LegalQueryStringParameterNames)
            {
                foreach (string queryStringParameterValue in HttpTestData.LegalQueryStringParameterValues)
                {
                    foreach (string mediaType in HttpTestData.LegalMediaTypeStrings)
                    {
                        QueryStringMapping mapping = new QueryStringMapping(queryStringParameterName, queryStringParameterValue, mediaType);
                        foreach (string uriBase in TestData.UriTestDataStrings.Where((s) => !s.Contains('?')))
                        {
                            string uri = uriBase + "?" + "not" + queryStringParameterName + "=" + queryStringParameterValue;
                            HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Get, uri);
                            HttpResponseMessage response = new HttpResponseMessage() { RequestMessage = request };
                            Assert.IsFalse(mapping.SupportsMediaType(response), string.Format("SupportsMediaType should have returned true for '{0}'.", uri));
                        }
                    }
                }
            }
        }

        [TestMethod]
        [TestCategory("CIT"), Timeout(TimeoutConstant.DefaultTimeout), Owner("")]
        [Description("SupportsMediaType(HttpResponseMessage) returns false when the QueryStringParameterValue is not in the Uri.")]
        public void SupportsMediaType1ReturnsFalseWithQueryStringParameterValueNotInUri()
        {
            foreach (string queryStringParameterName in HttpTestData.LegalQueryStringParameterNames)
            {
                foreach (string queryStringParameterValue in HttpTestData.LegalQueryStringParameterValues)
                {
                    foreach (string mediaType in HttpTestData.LegalMediaTypeStrings)
                    {
                        QueryStringMapping mapping = new QueryStringMapping(queryStringParameterName, queryStringParameterValue, mediaType);
                        foreach (string uriBase in TestData.UriTestDataStrings.Where((s) => !s.Contains('?')))
                        {
                            string uri = uriBase + "?" + queryStringParameterName + "=" + "not" + queryStringParameterValue;
                            HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Get, uri);
                            HttpResponseMessage response = new HttpResponseMessage() { RequestMessage = request };
                            Assert.IsFalse(mapping.SupportsMediaType(response), string.Format("SupportsMediaType should have returned true for '{0}'.", uri));
                        }
                    }
                }
            }
        }

        [TestMethod]
        [TestCategory("CIT"), Timeout(TimeoutConstant.DefaultTimeout), Owner("")]
        [Description("SupportsMediaType(HttpResponseMessage) throws with a null HttpResponseMessage.")]
        public void SupportsMediaType1ThrowsWithNullHttpResponseMessage()
        {
            foreach (string queryStringParameterName in HttpTestData.LegalQueryStringParameterNames)
            {
                foreach (string queryStringParameterValue in HttpTestData.LegalQueryStringParameterValues)
                {
                    foreach (string mediaType in HttpTestData.LegalMediaTypeStrings)
                    {
                        QueryStringMapping mapping = new QueryStringMapping(queryStringParameterName, queryStringParameterValue, mediaType);
                        ExceptionAssert.ThrowsArgumentNull("response", () => mapping.SupportsMediaType((HttpResponseMessage)null));
                    }
                }
            }
        }


        [TestMethod]
        [TestCategory("CIT"), Timeout(TimeoutConstant.DefaultTimeout), Owner("")]
        [Description("SupportsMediaType(HttpResponseMessage) throws with a null HttpRequestMessage in the HttpResponseMessage.")]
        public void SupportsMediaType1ThrowsWithNullRequestInHttpResponseMessage()
        {
            foreach (string queryStringParameterName in HttpTestData.LegalQueryStringParameterNames)
            {
                foreach (string queryStringParameterValue in HttpTestData.LegalQueryStringParameterValues)
                {
                    foreach (string mediaType in HttpTestData.LegalMediaTypeStrings)
                    {
                        QueryStringMapping mapping = new QueryStringMapping(queryStringParameterName, queryStringParameterValue, mediaType);
                        string errorMessage = SR.ResponseMustReferenceRequest(typeof(HttpResponseMessage).Name, "response", typeof(HttpRequestMessage).Name, "RequestMessage");
                        ExceptionAssert.Throws<InvalidOperationException>("Null request in response should throw.", errorMessage, () => mapping.SupportsMediaType(new HttpResponseMessage()));
                    }
                }
            }
        }

        [TestMethod]
        [TestCategory("CIT"), Timeout(TimeoutConstant.DefaultTimeout), Owner("")]
        [Description("SupportsMediaType(HttpResponseMessage) throws with a null Uri in HttpRequestMessage.")]
        public void SupportsMediaType1ThrowsWithNullUriInHttpRequestMessage()
        {
            foreach (string queryStringParameterName in HttpTestData.LegalQueryStringParameterNames)
            {
                foreach (string queryStringParameterValue in HttpTestData.LegalQueryStringParameterValues)
                {
                    foreach (string mediaType in HttpTestData.LegalMediaTypeStrings)
                    {
                        QueryStringMapping mapping = new QueryStringMapping(queryStringParameterName, queryStringParameterValue, mediaType);
                        string errorMessage = SR.NonNullUriRequiredForMediaTypeMapping(this.TypeUnderTest.Name);
                        HttpRequestMessage request = new HttpRequestMessage();
                        HttpResponseMessage response = new HttpResponseMessage() { RequestMessage = request };
                        ExceptionAssert.Throws<InvalidOperationException>("Null Uri should throw.", errorMessage, () => mapping.SupportsMediaType(response));
                    }
                }
            }
        }

        #endregion SupportsMediaType(HttpResponseMessage)

        #endregion Methods
    }
}
