// <copyright>
//   Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>

namespace Microsoft.ApplicationServer.Http
{
    using System;
    using System.IO;
    using System.Net.Http;
    using System.Net.Http.Headers;
    using System.Threading.Tasks;
    using Microsoft.ApplicationServer.Common.Test;
    using Microsoft.ApplicationServer.Http.Dispatcher;
    using Microsoft.ApplicationServer.Http.Test;
    using Microsoft.VisualStudio.TestTools.UnitTesting;

    [TestClass, UnitTestLevel(UnitTestLevel.InProgress)]
    public class UriPathExtensionMappingTests : UnitTest<UriPathExtensionMapping>
    {
        #region Type

        [TestMethod]
        [TestCategory("CIT"), Timeout(TimeoutConstant.DefaultTimeout), Owner("")]
        [Description("UriPathExtensionMapping is public, concrete, and sealed.")]
        public void TypeIsCorrect()
        {
            Type t = this.TypeUnderTest;
            Assert.IsTrue(t.IsPublic, "UriPathExtensionMapping should be public.");
            Assert.IsFalse(t.IsAbstract, "UriPathExtensionMapping should not be abstract.");
            Assert.IsTrue(t.IsSealed, "UriPathExtensionMapping should be sealed.");
            Assert.AreEqual(typeof(MediaTypeMapping), t.BaseType, "UriPathExtensionMapping should derive from MediaTypeMapping.");
        }

        #endregion Type

        #region Constructors

        #region UriPathExtensionMapping(string, string)

        [TestMethod]
        [TestCategory("CIT"), Timeout(TimeoutConstant.DefaultTimeout), Owner("")]
        [Description("UriPathExtensionMapping(string, string) sets UriPathExtension and MediaType.")]
        public void Constructor()
        {
            foreach (string uriPathExtension in HttpTestData.LegalUriPathExtensions)
            {
                foreach (string mediaType in HttpTestData.LegalMediaTypeStrings)
                {
                    UriPathExtensionMapping mapping = new UriPathExtensionMapping(uriPathExtension, mediaType);
                    Assert.AreEqual(uriPathExtension, mapping.UriPathExtension, "Failed to set UriPathExtension.");
                    MediaTypeAssert.AreEqual(mediaType, mapping.MediaType, "Failed to set MediaType.");
                }
            }
        }

        [TestMethod]
        [TestCategory("CIT"), Timeout(TimeoutConstant.DefaultTimeout), Owner("")]
        [Description("UriPathExtensionMapping(string, string) throws if the UriPathExtensions parameter is null.")]
        public void ConstructorThrowsWithEmptyUriPathExtension()
        {
            foreach (string uriPathExtension in TestData.EmptyStrings)
            {
                foreach (string mediaType in HttpTestData.LegalMediaTypeStrings)
                {
                    ExceptionAssert.ThrowsArgumentNull("uriPathExtension", () => new UriPathExtensionMapping(uriPathExtension, mediaType));
                }
            };
        }

        [TestMethod]
        [TestCategory("CIT"), Timeout(TimeoutConstant.DefaultTimeout), Owner("")]
        [Description("UriPathExtensionMapping(string, string) throws if the MediaType (string) parameter is empty.")]
        public void ConstructorThrowsWithEmptyMediaType()
        {
            foreach (string uriPathExtension in HttpTestData.LegalUriPathExtensions)
            {
                foreach (string mediaType in TestData.EmptyStrings)
                {
                    ExceptionAssert.ThrowsArgumentNull("mediaType", () => new UriPathExtensionMapping(uriPathExtension, mediaType));
                }
            };
        }

        #endregion UriPathExtensionMapping(string, string)

        #region UriPathExtensionMapping(string, MediaTypeHeaderValue)

        [TestMethod]
        [TestCategory("CIT"), Timeout(TimeoutConstant.DefaultTimeout), Owner("")]
        [Description("UriPathExtensionMapping(string, MediaTypeHeaderValue) sets UriPathExtension and MediaType.")]
        public void Constructor1()
        {
            foreach (string uriPathExtension in HttpTestData.LegalUriPathExtensions)
            {
                foreach (MediaTypeHeaderValue mediaType in HttpTestData.LegalMediaTypeHeaderValues)
                {
                    UriPathExtensionMapping mapping = new UriPathExtensionMapping(uriPathExtension, mediaType);
                    Assert.AreEqual(uriPathExtension, mapping.UriPathExtension, "Failed to set UriPathExtension.");
                    MediaTypeAssert.AreEqual(mediaType, mapping.MediaType, "Failed to set MediaType.");
                }
            }
        }

        [TestMethod]
        [TestCategory("CIT"), Timeout(TimeoutConstant.DefaultTimeout), Owner("")]
        [Description("UriPathExtensionMapping(string, MediaTypeHeaderValue) throws if the UriPathExtensions parameter is null.")]
        public void Constructor1ThrowsWithEmptyUriPathExtension()
        {
            foreach (string uriPathExtension in TestData.EmptyStrings)
            {
                foreach (MediaTypeHeaderValue mediaType in HttpTestData.LegalMediaTypeHeaderValues)
                {
                    ExceptionAssert.ThrowsArgumentNull("uriPathExtension", () => new UriPathExtensionMapping(uriPathExtension, mediaType));
                }
            };
        }

        [TestMethod]
        [TestCategory("CIT"), Timeout(TimeoutConstant.DefaultTimeout), Owner("")]
        [Description("UriPathExtensionMapping constructor throws if the mediaType parameter is null.")]
        public void Constructor1ThrowsWithNullMediaType()
        {
            foreach (string uriPathExtension in HttpTestData.LegalUriPathExtensions)
            {
                ExceptionAssert.ThrowsArgumentNull("mediaType", () => new UriPathExtensionMapping(uriPathExtension, (MediaTypeHeaderValue)null));
            };
        }

        #endregion UriPathExtensionMapping(string, MediaTypeHeaderValue)

        #endregion  Constructors

        #region Properties
        #endregion Properties

        #region Methods

        #region SupportsMediaType(HttpRequestMessage)

        [TestMethod]
        [TestCategory("CIT"), Timeout(TimeoutConstant.DefaultTimeout), Owner("")]
        [Description("SupportsMediaType(HttpRequestMessage) returns true when the extension is in the Uri.")]
        public void SupportsMediaTypeReturnsTrueWithExtensionInUri()
        {
            foreach (string uriPathExtension in HttpTestData.LegalUriPathExtensions)
            {
                foreach (string mediaType in HttpTestData.LegalMediaTypeStrings)
                {
                    UriPathExtensionMapping mapping = new UriPathExtensionMapping(uriPathExtension, mediaType);
                    foreach (string baseUriString in TestData.UriTestDataStrings)
                    {
                        Uri baseUri = new Uri(baseUriString);
                        Uri uri = new Uri(baseUri, "x." + uriPathExtension);
                        HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Get, uri);
                        Assert.IsTrue(mapping.SupportsMediaType(request), string.Format("SupportsMediaType should have returned true for '{0}' and '{1}'.", mediaType, uri));
                    }
                }
            }
        }

        [TestMethod]
        [TestCategory("CIT"), Timeout(TimeoutConstant.DefaultTimeout), Owner("")]
        [Description("SupportsMediaType(HttpRequestMessage) returns false when the extension is not in the Uri.")]
        public void SupportsMediaTypeReturnsFalseWithExtensionNotInUri()
        {
            foreach (string uriPathExtension in HttpTestData.LegalUriPathExtensions)
            {
                foreach (string mediaType in HttpTestData.LegalMediaTypeStrings)
                {
                    UriPathExtensionMapping mapping = new UriPathExtensionMapping(uriPathExtension, mediaType);
                    foreach (string baseUriString in TestData.UriTestDataStrings)
                    {
                        Uri baseUri = new Uri(baseUriString);
                        Uri uri = new Uri(baseUri, "x.");
                        HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Get, uri);
                        Assert.IsFalse(mapping.SupportsMediaType(request), string.Format("SupportsMediaType should have returned false for '{0}' and '{1}'.", mediaType, uri));
                    }
                }
            }
        }

        [TestMethod]
        [TestCategory("CIT"), Timeout(TimeoutConstant.DefaultTimeout), Owner("")]
        [Description("SupportsMediaType(HttpRequestMessage) returns false when the uri contains the extension but does not end with it.")]
        public void SupportsMediaTypeReturnsFalseWithExtensionNotLastInUri()
        {
            foreach (string uriPathExtension in HttpTestData.LegalUriPathExtensions)
            {
                foreach (string mediaType in HttpTestData.LegalMediaTypeStrings)
                {
                    UriPathExtensionMapping mapping = new UriPathExtensionMapping(uriPathExtension, mediaType);
                    foreach (string baseUriString in TestData.UriTestDataStrings)
                    {
                        Uri baseUri = new Uri(baseUriString);
                        Uri uri = new Uri(baseUri, "x." + uriPathExtension + "z");
                        HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Get, uri);
                        Assert.IsFalse(mapping.SupportsMediaType(request), string.Format("SupportsMediaType should have returned false for '{0}' and '{1}'.", mediaType, uri));
                    }
                }
            }
        }

        [TestMethod]
        [TestCategory("CIT"), Timeout(TimeoutConstant.DefaultTimeout), Owner("")]
        [Description("SupportsMediaType(HttpRequestMessage) throws if the request is null.")]
        public void SupportsMediaTypeThrowsWithNullHttpRequestMessage()
        {
            foreach (string uriPathExtension in HttpTestData.LegalUriPathExtensions)
            {
                foreach (string mediaType in HttpTestData.LegalMediaTypeStrings)
                {
                    UriPathExtensionMapping mapping = new UriPathExtensionMapping(uriPathExtension, mediaType);
                    ExceptionAssert.ThrowsArgumentNull("request", () => mapping.SupportsMediaType((HttpRequestMessage)null));
                }
            }
        }

        [TestMethod]
        [TestCategory("CIT"), Timeout(TimeoutConstant.DefaultTimeout), Owner("")]
        [Description("SupportsMediaType(HttpRequestMessage) throws if the Uri in the request is null.")]
        public void SupportsMediaTypeThrowsWithNullUriInHttpRequestMessage()
        {
            foreach (string uriPathExtension in HttpTestData.LegalUriPathExtensions)
            {
                foreach (string mediaType in HttpTestData.LegalMediaTypeStrings)
                {
                    UriPathExtensionMapping mapping = new UriPathExtensionMapping(uriPathExtension, mediaType);
                    string errorMessage = SR.NonNullUriRequiredForMediaTypeMapping(this.TypeUnderTest.Name);
                    ExceptionAssert.Throws<InvalidOperationException>("Null Uri should throw.", errorMessage, () => mapping.SupportsMediaType(new HttpRequestMessage()));
                }
            }
        }

        #endregion SupportsMediaType(HttpRequestMessage)

        #region SupportsMediaType(HttpResponseMessage)

        [TestMethod]
        [TestCategory("CIT"), Timeout(TimeoutConstant.DefaultTimeout), Owner("")]
        [Description("SupportsMediaType(HttpResponseMessage) returns true when the extension is in the Uri.")]
        public void SupportsMediaType1ReturnsTrueWithExtensionInUri()
        {
            foreach (string uriPathExtension in HttpTestData.LegalUriPathExtensions)
            {
                foreach (string mediaType in HttpTestData.LegalMediaTypeStrings)
                {
                    UriPathExtensionMapping mapping = new UriPathExtensionMapping(uriPathExtension, mediaType);
                    foreach (string baseUriString in TestData.UriTestDataStrings)
                    {
                        Uri baseUri = new Uri(baseUriString);
                        Uri uri = new Uri(baseUri, "x." + uriPathExtension);
                        HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Get, uri);
                        HttpResponseMessage response = new HttpResponseMessage() { RequestMessage = request };
                        Assert.IsTrue(mapping.SupportsMediaType(request), string.Format("SupportsMediaType should have returned true for '{0}' and '{1}'.", mediaType, uri));
                    }
                }
            }
        }

        [TestMethod]
        [TestCategory("CIT"), Timeout(TimeoutConstant.DefaultTimeout), Owner("")]
        [Description("SupportsMediaType(HttpResponseMessage) returns false when the extension is not in the Uri.")]
        public void SupportsMediaType1ReturnsFalseWithExtensionNotInUri()
        {
            foreach (string uriPathExtension in HttpTestData.LegalUriPathExtensions)
            {
                foreach (string mediaType in HttpTestData.LegalMediaTypeStrings)
                {
                    UriPathExtensionMapping mapping = new UriPathExtensionMapping(uriPathExtension, mediaType);
                    foreach (string baseUriString in TestData.UriTestDataStrings)
                    {
                        Uri baseUri = new Uri(baseUriString);
                        Uri uri = new Uri(baseUri, "x.");
                        HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Get, uri);
                        HttpResponseMessage response = new HttpResponseMessage() { RequestMessage = request };
                        Assert.IsFalse(mapping.SupportsMediaType(response), string.Format("SupportsMediaType should have returned false for '{0}' and '{1}'.", mediaType, uri));
                    }
                }
            }
        }

        [TestMethod]
        [TestCategory("CIT"), Timeout(TimeoutConstant.DefaultTimeout), Owner("")]
        [Description("SupportsMediaType(HttpResponseMessage) returns false when the uri contains the extension but does not end with it.")]
        public void SupportsMediaType1ReturnsFalseWithExtensionNotLastInUri()
        {
            foreach (string uriPathExtension in HttpTestData.LegalUriPathExtensions)
            {
                foreach (string mediaType in HttpTestData.LegalMediaTypeStrings)
                {
                    UriPathExtensionMapping mapping = new UriPathExtensionMapping(uriPathExtension, mediaType);
                    foreach (string baseUriString in TestData.UriTestDataStrings)
                    {
                        Uri baseUri = new Uri(baseUriString);
                        Uri uri = new Uri(baseUri, "x." + uriPathExtension + "z");
                        HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Get, uri);
                        HttpResponseMessage response = new HttpResponseMessage() { RequestMessage = request };
                        Assert.IsFalse(mapping.SupportsMediaType(response), string.Format("SupportsMediaType should have returned false for '{0}' and '{1}'.", mediaType, uri));
                    }
                }
            }
        }

        [TestMethod]
        [TestCategory("CIT"), Timeout(TimeoutConstant.DefaultTimeout), Owner("")]
        [Description("SupportsMediaType(HttpResponseMessage) throws if the response is null.")]
        public void SupportsMediaType1ThrowsWithNullHttpResponseMessage()
        {
            foreach (string uriPathExtension in HttpTestData.LegalUriPathExtensions)
            {
                foreach (string mediaType in HttpTestData.LegalMediaTypeStrings)
                {
                    UriPathExtensionMapping mapping = new UriPathExtensionMapping(uriPathExtension, mediaType);
                    ExceptionAssert.ThrowsArgumentNull("response", () => mapping.SupportsMediaType((HttpResponseMessage)null));
                }
            }
        }

        [TestMethod]
        [TestCategory("CIT"), Timeout(TimeoutConstant.DefaultTimeout), Owner("")]
        [Description("SupportsMediaType(HttpResponseMessage) throws if the HttpRequestMessage in the HttpResponseMessage is null.")]
        public void SupportsMediaType1ThrowsWithNullRequestInHttpResponseMessage()
        {
            foreach (string uriPathExtension in HttpTestData.LegalUriPathExtensions)
            {
                foreach (string mediaType in HttpTestData.LegalMediaTypeStrings)
                {
                    UriPathExtensionMapping mapping = new UriPathExtensionMapping(uriPathExtension, mediaType);
                    string errorMessage = SR.ResponseMustReferenceRequest(typeof(HttpResponseMessage).Name, "response", typeof(HttpRequestMessage).Name, "RequestMessage");
                    ExceptionAssert.Throws<InvalidOperationException>("Null request in response should throw.", errorMessage, () => mapping.SupportsMediaType(new HttpResponseMessage()));
                }
            }
        }

        [TestMethod]
        [TestCategory("CIT"), Timeout(TimeoutConstant.DefaultTimeout), Owner("")]
        [Description("SupportsMediaType(HttpResponseMessage) throws if the Uri in the request is null.")]
        public void SupportsMediaType1ThrowsWithNullUriInHttpRequestMessage()
        {
            foreach (string uriPathExtension in HttpTestData.LegalUriPathExtensions)
            {
                foreach (string mediaType in HttpTestData.LegalMediaTypeStrings)
                {
                    UriPathExtensionMapping mapping = new UriPathExtensionMapping(uriPathExtension, mediaType);
                    HttpRequestMessage request = new HttpRequestMessage();
                    HttpResponseMessage response = new HttpResponseMessage() { RequestMessage = request };
                    string errorMessage = SR.NonNullUriRequiredForMediaTypeMapping(this.TypeUnderTest.Name);
                    ExceptionAssert.Throws<InvalidOperationException>("Null Uri should throw.", errorMessage, () => mapping.SupportsMediaType(response));
                }
            }
        }
        #endregion SupportsMediaType(HttpResponseMessage)

        #endregion Methods
    }
}
