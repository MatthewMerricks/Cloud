// <copyright>
//   Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>

namespace Microsoft.ApplicationServer.Http
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Net.Http;
    using System.Reflection;
    using Microsoft.ApplicationServer.Common.Test;
    using Microsoft.ApplicationServer.Http.Test;
    using Microsoft.VisualStudio.TestTools.UnitTesting;

    [TestClass, UnitTestLevel(UnitTestLevel.InProgress)]
    public class HttpRequestMessageOfTTests : UnitTest<HttpRequestMessage<object>>
    {
        private static readonly Type objectContentOfTType = typeof(ObjectContent<>);
        private static readonly Type httpRequestMessageOfTType = typeof(HttpRequestMessage<>);

        #region Type

        [TestMethod]
        [TestCategory("CIT"), Timeout(TimeoutConstant.DefaultTimeout), Owner("")]
        [Description("HttpRequestMessage<T> is public, concrete, unsealed and generic.")]
        public void TypeIsCorrect()
        {
            Type t = this.TypeUnderTest;
            Assert.IsTrue(t.IsPublic, "HttpRequestMessage<T> should be public.");
            Assert.IsFalse(t.IsAbstract, "HttpRequestMessage<T> should not be abstract.");
            Assert.IsFalse(t.IsSealed, "HttpRequestMessage should<T> not be sealed.");
            Assert.IsTrue(t.IsGenericType, "HttpRequestMessage<T> should be generic.");
            Assert.AreEqual(typeof(HttpRequestMessage), typeof(HttpRequestMessage<>).BaseType, "HttpRequestMessage<T> base type should be HttpRequestMessage.");
        }

        #endregion Type

        #region Constructors

        #region HttpRequestMessage<T>()

        [Ignore]
        [TestMethod]
        [TestCategory("CIT"), Timeout(TimeoutConstant.DefaultTimeout), Owner("")]
        [Description("HttpRequestMessage<T>() works with all known value and reference types.")]
        public void Constructor()
        {
            TestDataAssert.Execute(
                TestData.RepresentativeValueAndRefTypeTestDataCollection,
                (type, obj) =>
                {
                    HttpRequestMessage request = GenericTypeAssert.InvokeConstructor<HttpRequestMessage>(
                                                    httpRequestMessageOfTType, 
                                                    type);

                    GenericTypeAssert.IsCorrectGenericType<HttpRequestMessage>(request, type);
                    Assert.IsNotNull(request.Content, "default contructor should have set Content.");
                });
        }

        #endregion HttpRequestMessage<T>()

        #region HttpRequestMessage<T>(T)

        [TestMethod]
        [TestCategory("CIT"), Timeout(TimeoutConstant.DefaultTimeout), Owner("")]
        [Description("HttpRequestMessage<T>(T) sets Content property with all known value and reference types.")]
        public void Constructor1()
        {
            TestDataAssert.Execute(
                TestData.RepresentativeValueAndRefTypeTestDataCollection,
                (type, obj) =>
                {
                    HttpRequestMessage request = GenericTypeAssert.InvokeConstructor<HttpRequestMessage>(
                                                    httpRequestMessageOfTType,
                                                    type,
                                                    new Type[] { type },
                                                    new object[] { obj });

                    GenericTypeAssert.IsCorrectGenericType<HttpRequestMessage>(request, type);
                    ObjectContentAssert.IsCorrectGenericType(request.Content as ObjectContent, type);
                });
        }

        #endregion HttpRequestMessage<T>(T)

        #region HttpRequestMessage<T>(T, IEnumerable<MediaTypeFormatter>)

        [TestMethod]
        [TestCategory("CIT"), Timeout(TimeoutConstant.DefaultTimeout), Owner("")]
        [Description("HttpRequestMessage<T>(T, IEnumerable<MediaTypeFormatter>) sets Content property with all known value and reference types.")]
        public void Constructor2()
        {
            TestDataAssert.Execute(
                TestData.RepresentativeValueAndRefTypeTestDataCollection,
                (type, obj) =>
                {
                    foreach (IEnumerable<MediaTypeFormatter> formatterCollection in HttpTestData.AllFormatterCollections)
                    {
                        MediaTypeFormatter[] formatters = formatterCollection.ToArray();
                        HttpRequestMessage request = GenericTypeAssert.InvokeConstructor<HttpRequestMessage>(
                                                        httpRequestMessageOfTType,
                                                        type,
                                                        new Type[] { type, typeof(IEnumerable<MediaTypeFormatter>) },
                                                        new object[] { obj, formatters });

                        GenericTypeAssert.IsCorrectGenericType<HttpRequestMessage>(request, type);
                        ObjectContentAssert.IsCorrectGenericType(request.Content as ObjectContent, type);
                        ObjectContentAssert.ContainsFormatters(request.Content as ObjectContent, formatters);
                    }
                });
        }

        [TestMethod]
        [TestCategory("CIT"), Timeout(TimeoutConstant.DefaultTimeout), Owner("")]
        [Description("HttpRequestMessage<T>(T, IEnumerable<MediaTypeFormatter>) throws with null formatters parameter.")]
        public void Constructor2ThrowsWithNullFormatters()
        {
            TestDataAssert.Execute(
                TestData.RepresentativeValueAndRefTypeTestDataCollection,
                (type, obj) =>
                {
                    ExceptionAssert.ThrowsArgumentNull<TargetInvocationException>(
                        "formatters",
                    () =>
                    {
                        HttpRequestMessage request = GenericTypeAssert.InvokeConstructor<HttpRequestMessage>(
                                                        httpRequestMessageOfTType,
                                                        type,
                                                        new Type[] { type, typeof(IEnumerable<MediaTypeFormatter>) },
                                                        new object[] { obj, null });
                    });
                });
        }

        #endregion HttpRequestMessage<T>(T, IEnumerable<MediaTypeFormatter>)

        #region HttpRequestMessage<T>(T, HttpMethod, Uri, IEnumerable<MediaTypeFormatter>)

        [TestMethod]
        [TestCategory("CIT"), Timeout(TimeoutConstant.DefaultTimeout), Owner("")]
        [Description("HttpRequestMessage<T>(T, HttpMethod, Uri, IEnumerable<MediaTypeFormatter>) sets Content, Method and Uri properties with all known value and reference types.")]
        public void Constructor3()
        {
            TestDataAssert.Execute(
                TestData.RepresentativeValueAndRefTypeTestDataCollection,
                (type, obj) =>
                {
                    foreach (HttpMethod httpMethod in HttpTestData.AllHttpMethods)
                    {
                        foreach (Uri uri in TestData.UriTestData)
                        {
                            foreach (IEnumerable<MediaTypeFormatter> formatterCollection in HttpTestData.AllFormatterCollections)
                            {
                                MediaTypeFormatter[] formatters = formatterCollection.ToArray();
                                HttpRequestMessage request = GenericTypeAssert.InvokeConstructor<HttpRequestMessage>(
                                                                httpRequestMessageOfTType,
                                                                type,
                                                                new Type[] { type, typeof(HttpMethod), typeof(Uri), typeof(IEnumerable<MediaTypeFormatter>) },
                                                                new object[] { obj, httpMethod, uri, formatters });

                                GenericTypeAssert.IsCorrectGenericType<HttpRequestMessage>(request, type);
                                Assert.AreEqual(uri, request.RequestUri, "Uri property was not set.");
                                Assert.AreEqual(httpMethod, request.Method, "Method property was not set.");
                                ObjectContentAssert.IsCorrectGenericType(request.Content as ObjectContent, type);
                                ObjectContentAssert.ContainsFormatters(request.Content as ObjectContent, formatters);
                            }
                        }
                    }
                });
        }

        [TestMethod]
        [TestCategory("CIT"), Timeout(TimeoutConstant.DefaultTimeout), Owner("")]
        [Description("HttpRequestMessage<T>(T, HttpMethod, Uri, IEnumerable<MediaTypeFormatter>) throws with null formatters.")]
        public void Constructor3ThrowsWithNullFormatters()
        {
            TestDataAssert.Execute(
                TestData.RepresentativeValueAndRefTypeTestDataCollection,
                (type, obj) =>
                {
                    foreach (HttpMethod httpMethod in HttpTestData.AllHttpMethods)
                    {
                        foreach (Uri uri in TestData.UriTestData)
                        {
                            ExceptionAssert.ThrowsArgumentNull<TargetInvocationException>(
                                "formatters",
                                () =>
                                {
                                    HttpRequestMessage request = GenericTypeAssert.InvokeConstructor<HttpRequestMessage>(
                                                                    httpRequestMessageOfTType,
                                                                    type,
                                                                    new Type[] { type, typeof(HttpMethod), typeof(Uri), typeof(IEnumerable<MediaTypeFormatter>) },
                                                                    new object[] { obj, httpMethod, uri, null });
                                });
                        }
                    }
                });
        }

        #endregion HttpRequestMessage<T>(T, HttpMethod, Uri, IEnumerable<MediaTypeFormatter>)

        #endregion Constructors

        #region Properties

        [TestMethod]
        [TestCategory("CIT"), Timeout(TimeoutConstant.DefaultTimeout), Owner("")]
        [Description("HttpRequestMessage<T>.Content is settable.")]
        public void ContentIsSettable()
        {
            ObjectContent<string> content = new ObjectContent<string>("data");
            HttpRequestMessage<string> request = new HttpRequestMessage<string>();
            request.Content = content;
            Assert.AreSame(content, request.Content, "Content was not set.");
        }

        [TestMethod]
        [TestCategory("CIT"), Timeout(TimeoutConstant.DefaultTimeout), Owner("")]
        [Description("HttpRequestMessage<T>.Content setter sets Content.HttpRequestMessage to maintain pairing.")]
        public void ContentSetterSetsContentHttpRequestMessage()
        {
            ObjectContent<string> content = new ObjectContent<string>("data");
            HttpRequestMessage<string> request = new HttpRequestMessage<string>();
            request.Content = content;
            Assert.AreSame(request, content.HttpRequestMessage, "Failed to set Content.HttpRequestMessage.");
        }

        [TestMethod]
        [TestCategory("CIT"), Timeout(TimeoutConstant.DefaultTimeout), Owner("")]
        [Description("HttpRequestMessage<T>.Content getter sets Content.HttpRequestMessage if not set.")]
        public void ContentGetterSetsContentHttpRequestMessage()
        {
            ObjectContent<string> content = new ObjectContent<string>("data");
            HttpRequestMessage<string> request = new HttpRequestMessage<string>();

            // assign via base HttpRequestMessage to bypass our strongly typed setter
            HttpRequestMessage baseRequest = (HttpRequestMessage)request;
            baseRequest.Content = content;
            Assert.IsNull(content.HttpRequestMessage, "Content.HttpRequestMessage should be null before it is automatically repaired.");
            Assert.AreSame(request, request.Content.HttpRequestMessage, "Content.HttpRequestMessage should have been set via getter.");
        }

        [TestMethod]
        [TestCategory("CIT"), Timeout(TimeoutConstant.DefaultTimeout), Owner("")]
        [Description("HttpRequestMessage<T>.Content getter discovers non-ObjectContent<T> types and wraps them in ObjectContent<T>.")]
        public void ContentGetterSetsNewObjectContentOfTWithBaseHttpContent()
        {
            StringContent stringContent = new StringContent("data");
            HttpRequestMessage<string> request = new HttpRequestMessage<string>();

            // assign via base HttpRequestMessage to bypass our strongly typed setter
            HttpRequestMessage baseRequest = (HttpRequestMessage)request;
            baseRequest.Content = stringContent;

            ObjectContent<string> objectContent = request.Content;
            Assert.IsNotNull(objectContent, "Failed to create wrapper ObjectContent<T>");
            Assert.AreSame(request, objectContent.HttpRequestMessage, "Failed to pair new wrapper.");
        }

        #endregion Properties
    }
}
