// <copyright>
//   Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>

namespace Microsoft.ApplicationServer.Http
{
    using System;
    using System.Collections.Generic;
    using System.Collections.ObjectModel;
    using System.IO;
    using System.Linq;
    using System.Net.Http;
    using System.Net.Http.Headers;
    using System.Net.Http.Moles;
    using System.Runtime.Serialization;
    using System.Threading.Tasks;
    using System.Xml.Serialization;
    using Microsoft.ApplicationServer.Common.Test;
    using Microsoft.ApplicationServer.Http.Moles;
    using Microsoft.ApplicationServer.Http.Test;
    using Microsoft.VisualStudio.TestTools.UnitTesting;

    [TestClass, UnitTestLevel(UnitTestLevel.InProgress)]
    public class ObjectContentTests: UnitTest<ObjectContent>
    {
        [TestCleanup, HostType("Moles")]
        public void TestCleanup()
        {
            // Ensure every test resets Moled ObjectContent back to default
            MObjectContent.BehaveAsCurrent();
        }

        #region Type

        [TestMethod]
        [TestCategory("CIT"), Timeout(TimeoutConstant.DefaultTimeout), Owner("")]
        [Description("ObjectContent is public, concrete, and unsealed.")]
        public void TypeIsCorrect()
        {
            Type t = this.TypeUnderTest;
            Assert.IsTrue(t.IsPublic, "ObjectContent should be public");
            Assert.IsFalse(t.IsAbstract, "ObjectContent should not be abstract");
            Assert.IsFalse(t.IsSealed, "ObjectContent should not be sealed");
            Assert.AreEqual(typeof(HttpContent), this.TypeUnderTest.BaseType, "ObjectContent should derive from HttpContent");
        }

        #endregion Type

        #region Constructors

        #region ObjectContent(Type, object)

        [TestMethod, HostType("Moles")]
        [TestCategory("CIT"), Timeout(TimeoutConstant.DefaultTimeout), Owner("")]
        [Description("ObjectContent(Type, object) sets Type (private) ObjectInstance properties.  ContentType defaults to null.")]
        public void Constructor()
        {
            object ctorObject = null;
            MObjectContent.AllInstances.ValueSetObject = (@this, o) => ctorObject = o;

            TestDataAssert.Execute(
                TestData.RepresentativeValueAndRefTypeTestDataCollection,
                (type, obj) =>
                {
                    ObjectContent content = new ObjectContent(type, obj);
                    Assert.AreSame(type, content.Type, "Failed to set Type");
                    Assert.IsNull(content.Headers.ContentType, "Content type should default to null.");
                    Assert.AreEqual(obj, ctorObject, "Failed to set ObjectInstance");
                });
        }

        [TestMethod, HostType("Moles")]
        [TestCategory("CIT"), Timeout(TimeoutConstant.DefaultTimeout), Owner("")]
        [Description("ObjectContent(Type, object) sets HttpContent property with object parameter==HttpContent.")]
        public void ConstructorSetsHttpContentWithHttpContentAsObject()
        {
            object ctorHttpContent = null;
            MObjectContent.AllInstances.HttpContentSetHttpContent = (@this, o) => ctorHttpContent = o;

            foreach (HttpContent httpContent in HttpTestData.StandardHttpContents)
            {
                ObjectContent content = new ObjectContent(typeof(string), (object)httpContent);
                Assert.AreEqual(httpContent, ctorHttpContent, "Failed to set HttpContent");
            };
        }

        [TestMethod, HostType("Moles")]
        [TestCategory("CIT"), Timeout(TimeoutConstant.DefaultTimeout), Owner("")]
        [Description("ObjectContent(Type, object) sets HttpContent property to a StreamContent when Object parameter is a Stream.")]
        public void ConstructorSetsHttpContentWithStreamAsObject()
        {
            object ctorHttpContent = null;
            MObjectContent.AllInstances.HttpContentSetHttpContent = (@this, o) => ctorHttpContent = o;

            StreamAssert.UsingXmlSerializer<int>(
                5,
                (stream) =>
                {
                    ObjectContent content = new ObjectContent(typeof(int), stream);
                    StreamContent streamContent = ctorHttpContent as StreamContent;
                    Assert.IsNotNull(streamContent, "Stream was not wrapped in StreamContent.");
                    XmlSerializer serializer = new XmlSerializer(typeof(int));
                    int result = (int) serializer.Deserialize(streamContent.ContentReadStream);
                    Assert.AreEqual(5, result, "Expected stream to deserialize to this value.");
                });
        }

        [TestMethod]
        [TestCategory("CIT"), Timeout(TimeoutConstant.DefaultTimeout), Owner("")]
        [Description("ObjectContent(Type, object) throws for a null Type parameter.")]
        public void ConstructorThrowsWithNullType()
        {
            ExceptionAssert.ThrowsArgumentNull("type", () => new ObjectContent((Type)null, 5));
        }

        [TestMethod]
        [TestCategory("CIT"), Timeout(TimeoutConstant.DefaultTimeout), Owner("")]
        [Description("ObjectContent(Type, object) throws for a null value type.")]
        public void ConstructorThrowsWithNullValueTypeObject()
        {
            TestDataAssert.Execute(
                TestData.ValueTypeTestDataCollection,
                TestDataVariations.AsInstance,
                "Null value types should throw",
                (type, obj) =>
                {
                    ExceptionAssert.Throws<InvalidOperationException>(
                        SR.CannotUseNullValueType(typeof(ObjectContent).Name, type.Name),
                        () => new ObjectContent(type, (object)null));
                });
        }

        [TestMethod]
        [TestCategory("CIT"), Timeout(TimeoutConstant.DefaultTimeout), Owner("")]
        [Description("ObjectContent(Type, object) throws if object is not assignable to Type.")]
        public void ConstructorThrowsWithObjectNotAssignableToType()
        {
            TestDataAssert.Execute(
                TestData.RepresentativeValueAndRefTypeTestDataCollection,
                (type, obj) =>
                {
                    if (obj != null)
                    {
                        Type mismatchingType = (type == typeof(string)) ? typeof(int) : typeof(string);
                        ExceptionAssert.ThrowsArgument(
                            "value",
                            SR.ObjectAndTypeDisagree(obj.GetType().Name, mismatchingType.Name),
                            () => new ObjectContent(mismatchingType, obj));
                    }
                });
        }

        #endregion ObjectContent(Type, object)

        #region ObjectContent(Type, object, string)

        [TestMethod, HostType("Moles")]
        [TestCategory("CIT"), Timeout(TimeoutConstant.DefaultTimeout), Owner("")]
        [Description("ObjectContent(Type, object, string) sets Type, content header's media type, and (private) ObjectInstance properties.")]
        public void Constructor1()
        {
            object ctorObject = null;
            MObjectContent.AllInstances.ValueSetObject = (@this, o) => ctorObject = o;

            TestDataAssert.Execute(
                TestData.RepresentativeValueAndRefTypeTestDataCollection,
                (type, obj) =>
                {
                    foreach (string mediaType in HttpTestData.LegalMediaTypeStrings)
                    {
                        ObjectContent content = new ObjectContent(type, obj, mediaType);
                        Assert.AreSame(type, content.Type, "Failed to set Type.");
                        MediaTypeAssert.AreEqual(content.Headers.ContentType, mediaType, "MediaType was not set.");
                        Assert.AreEqual(obj, ctorObject, "Failed to set ObjectInstance.");
                    };
                });
        }

        [TestMethod, HostType("Moles")]
        [TestCategory("CIT"), Timeout(TimeoutConstant.DefaultTimeout), Owner("")]
        [Description("ObjectContent(Type, object, string) sets HttpContent property with object parameter==HttpContent.")]
        public void Constructor1SetsHttpContentWithHttpContentAsObject()
        {
            HttpContent ctorHttpContent = null;
            MObjectContent.AllInstances.HttpContentSetHttpContent = (@this, o) => ctorHttpContent = o;

            foreach (HttpContent httpContent in HttpTestData.StandardHttpContents)
            {
                foreach (string mediaType in HttpTestData.LegalMediaTypeStrings)
                {
                    ObjectContent content = new ObjectContent(typeof(string), (object)httpContent, mediaType);
                    Assert.AreEqual(httpContent, ctorHttpContent, "Failed to set HttpContent.");
                };
            };
        }

        [TestMethod, HostType("Moles")]
        [TestCategory("CIT"), Timeout(TimeoutConstant.DefaultTimeout), Owner("")]
        [Description("ObjectContent(Type, object, string) sets HttpContent property to a StreamContent when Object parameter is a Stream.")]
        public void Constructor1SetsHttpContentWithStreamAsObject()
        {
            object ctorHttpContent = null;
            MObjectContent.AllInstances.HttpContentSetHttpContent = (@this, o) => ctorHttpContent = o;

            foreach (string mediaType in HttpTestData.LegalMediaTypeStrings)
            {
                StreamAssert.UsingXmlSerializer<int>(
                    5,
                    (stream) =>
                    {
                        ObjectContent content = new ObjectContent(typeof(int), stream, mediaType);
                        StreamContent streamContent = ctorHttpContent as StreamContent;
                        Assert.IsNotNull(streamContent, "Stream was not wrapped in StreamContent.");
                        XmlSerializer serializer = new XmlSerializer(typeof(int));
                        int result = (int)serializer.Deserialize(streamContent.ContentReadStream);
                        Assert.AreEqual(5, result, "Expected stream to deserialize to this value.");
                    });
            }
        }

        [TestMethod]
        [TestCategory("CIT"), Timeout(TimeoutConstant.DefaultTimeout), Owner("")]
        [Description("ObjectContent(Type, object, string) throws for a null Type parameter.")]
        public void Constructor1ThrowsWithNullType()
        {
            foreach (string mediaType in HttpTestData.LegalMediaTypeStrings)
            {
                ExceptionAssert.ThrowsArgumentNull("type", () => new ObjectContent((Type)null, 5, mediaType));
            }
        }

        [TestMethod]
        [TestCategory("CIT"), Timeout(TimeoutConstant.DefaultTimeout), Owner("")]
        [Description("ObjectContent(Type, object, string) throws for a null value type.")]
        public void Constructor1ThrowsWithNullValueTypeObject()
        {
            TestDataAssert.Execute(
                TestData.ValueTypeTestDataCollection,
                TestDataVariations.AsInstance,
                "Null value types should throw",
                (type, obj) =>
                {
                    ExceptionAssert.Throws<InvalidOperationException>(
                        SR.CannotUseNullValueType(typeof(ObjectContent).Name, type.Name),
                        () => new ObjectContent(type, (object)null, "application/xml"));
                });
        }

        [TestMethod]
        [TestCategory("CIT"), Timeout(TimeoutConstant.DefaultTimeout), Owner("")]
        [Description("ObjectContent(Type, object, string) throws for an empty media type.")]
        public void Constructor1ThrowsWithEmptyMediaType()
        {
            TestDataAssert.Execute(
                TestData.RepresentativeValueAndRefTypeTestDataCollection,
                (type, obj) =>
                {
                    foreach (string mediaType in TestData.EmptyStrings)
                    {
                        ExceptionAssert.ThrowsArgumentNull(
                            "mediaType",
                            () => new ObjectContent(type, obj, mediaType));
                    };
                });
        }

        [TestMethod]
        [TestCategory("CIT"), Timeout(TimeoutConstant.DefaultTimeout), Owner("")]
        [Description("ObjectContent(Type, object, string) throws for an empty media type.")]
        public void Constructor1ThrowsWithIllegalMediaType()
        {
            TestDataAssert.Execute(
                TestData.RepresentativeValueAndRefTypeTestDataCollection,
                (type, obj) =>
                {
                    foreach (string mediaType in HttpTestData.IllegalMediaTypeStrings)
                    {
                        ExceptionAssert.ThrowsArgument(
                            "mediaType",
                            SR.InvalidMediaType(mediaType, typeof(MediaTypeHeaderValue).Name),
                            () => new ObjectContent(type, obj, mediaType));
                    };
                });
        }

        [TestMethod]
        [TestCategory("CIT"), Timeout(TimeoutConstant.DefaultTimeout), Owner("")]
        [Description("ObjectContent(Type, object, string) throws if object is not assignable to Type.")]
        public void Constructor1ThrowsWithObjectNotAssignableToType()
        {
            TestDataAssert.Execute(
                TestData.RepresentativeValueAndRefTypeTestDataCollection,
                (type, obj) =>
                {
                    if (obj != null)
                    {
                        foreach (string mediaType in HttpTestData.LegalMediaTypeStrings)
                        {
                            Type mismatchingType = (type == typeof(string)) ? typeof(int) : typeof(string);
                            ExceptionAssert.ThrowsArgument(
                                "value",
                                SR.ObjectAndTypeDisagree(obj.GetType().Name, mismatchingType.Name),
                                () => new ObjectContent(mismatchingType, obj, mediaType));
                        }
                    }
                });
        }

        #endregion ObjectContent(Type, object, string)

        #region ObjectContent(Type, object, MediaTypeHeaderValue)

        [TestMethod, HostType("Moles")]
        [TestCategory("CIT"), Timeout(TimeoutConstant.DefaultTimeout), Owner("")]
        [Description("ObjectContent(Type, object, MediaTypeHeaderValue) sets Type, content header's media type and (private) ObjectInstance properties.")]
        public void Constructor2()
        {
            object ctorObject = null;
            MObjectContent.AllInstances.ValueSetObject = (@this, o) => ctorObject = o;

            TestDataAssert.Execute(
                TestData.RepresentativeValueAndRefTypeTestDataCollection,
                (type, obj) =>
                {
                    foreach (MediaTypeHeaderValue mediaType in HttpTestData.LegalMediaTypeHeaderValues)
                    {
                        ObjectContent content = new ObjectContent(type, obj, mediaType);
                        Assert.AreSame(type, content.Type, "Failed to set Type");
                        MediaTypeAssert.AreEqual(content.Headers.ContentType, mediaType, "MediaType was not set.");
                        Assert.AreEqual(obj, ctorObject, "Failed to set ObjectInstance");
                    };
                });
        }

        [TestMethod]
        [TestCategory("CIT"), Timeout(TimeoutConstant.DefaultTimeout), Owner("")]
        [Description("ObjectContent(Type, object, MediaTypeHeaderValue) throws for a null Type parameter.")]
        public void Constructor2ThrowsWithNullType()
        {
            foreach (MediaTypeHeaderValue mediaType in HttpTestData.LegalMediaTypeHeaderValues)
            {
                ExceptionAssert.ThrowsArgumentNull("type", () => new ObjectContent((Type)null, 5, mediaType));
            }
        }

        [TestMethod]
        [TestCategory("CIT"), Timeout(TimeoutConstant.DefaultTimeout), Owner("")]
        [Description("ObjectContent(Type, object, MediaTypeHeaderValue) throws for a null value type.")]
        public void Constructor2ThrowsWithNullValueTypeObject()
        {
            TestDataAssert.Execute(
                TestData.ValueTypeTestDataCollection,
                TestDataVariations.AsInstance,
                "Null value types should throw",
                (type, obj) =>
                {
                    ExceptionAssert.Throws<InvalidOperationException>(
                        SR.CannotUseNullValueType(typeof(ObjectContent).Name, type.Name),
                        () => new ObjectContent(type, (object)null, new MediaTypeHeaderValue("application/xml")));
                });
        }

        [TestMethod]
        [TestCategory("CIT"), Timeout(TimeoutConstant.DefaultTimeout), Owner("")]
        [Description("ObjectContent(Type, object, MediaTypeHeaderValue) throws for a null media type.")]
        public void Constructor2ThrowsWithNullMediaType()
        {
            ExceptionAssert.ThrowsArgumentNull(
                "mediaType",
                () => new ObjectContent(typeof(int), 5, (MediaTypeHeaderValue)null));
        }

        [TestMethod]
        [TestCategory("CIT"), Timeout(TimeoutConstant.DefaultTimeout), Owner("")]
        [Description("ObjectContent(Type, object, MediaTypeHeaderValue) throws if object is not assignable to Type.")]
        public void Constructor2ThrowsWithObjectNotAssignableToType()
        {
            TestDataAssert.Execute(
                TestData.RepresentativeValueAndRefTypeTestDataCollection,
                (type, obj) =>
                {
                    if (obj != null)
                    {
                        foreach (MediaTypeHeaderValue mediaType in HttpTestData.LegalMediaTypeHeaderValues)
                        {
                            Type mismatchingType = (type == typeof(string)) ? typeof(int) : typeof(string);
                            ExceptionAssert.ThrowsArgument(
                                "value",
                                SR.ObjectAndTypeDisagree(obj.GetType().Name, mismatchingType.Name),
                                () => new ObjectContent(mismatchingType, obj, mediaType));
                        }
                    }
                });
        }

        #endregion ObjectContent(Type, object, MediaTypeHeaderValue)

        #region ObjectContent(Type, HttpContent)

        [TestMethod, HostType("Moles")]
        [TestCategory("CIT"), Timeout(TimeoutConstant.DefaultTimeout), Owner("")]
        [Description("ObjectContent(Type, HttpContent) sets Type and HttpContent properties and sets MediaType from HttpContent.")]
        public void Constructor3()
        {
            object ctorHttpContent = null;
            MObjectContent.AllInstances.HttpContentSetHttpContent = (@this, o) => ctorHttpContent = o;

            TestDataAssert.Execute(
                TestData.RepresentativeValueAndRefTypeTestDataCollection,
                (type, obj) =>
                {
                    foreach (HttpContent httpContent in HttpTestData.StandardHttpContents)
                    {
                        ObjectContent content = new ObjectContent(type, httpContent);
                        Assert.AreSame(type, content.Type, "Failed to set Type");
                        Assert.AreSame(httpContent, ctorHttpContent, "Failed to set HttpContent");
                        MediaTypeAssert.AreEqual(content.Headers.ContentType, httpContent.Headers.ContentType, "MediaType was not set.");
                    }
                });
        }

        [TestMethod, HostType("Moles")]
        [TestCategory("CIT"), Timeout(TimeoutConstant.DefaultTimeout), Owner("")]
        [Description("ObjectContent(Type, HttpContent) sets ContentHeaders from the HttpContent.")]
        public void Constructor3SetsContentHeadersWithHttpContent()
        {
            object ctorHttpContent = null;
            MObjectContent.AllInstances.HttpContentSetHttpContent = (@this, o) => ctorHttpContent = o;

            TestDataAssert.Execute(
                TestData.RepresentativeValueAndRefTypeTestDataCollection,
                (type, obj) =>
                {
                    foreach (HttpContent httpContent in HttpTestData.StandardHttpContents)
                    {
                        httpContent.Headers.Add("CIT-Header", "CIT-Value");
                        ObjectContent content = new ObjectContent(type, httpContent);
                        HttpAssert.Contains(content.Headers, "CIT-Header", "CIT-Value");
                    }
                });
        }

        [TestMethod]
        [TestCategory("CIT"), Timeout(TimeoutConstant.DefaultTimeout), Owner("")]
        [Description("ObjectContent(Type, HttpContent) throws for a null Type parameter.")]
        public void Constructor3ThrowsWithNullType()
        {
            foreach (HttpContent httpContent in HttpTestData.StandardHttpContents)
            {
                ExceptionAssert.ThrowsArgumentNull("type", () => new ObjectContent((Type)null, httpContent));
            }
        }

        [TestMethod]
        [TestCategory("CIT"), Timeout(TimeoutConstant.DefaultTimeout), Owner("")]
        [Description("ObjectContent(Type, HttpContent) throws for a null HttpContent.")]
        public void Constructor3ThrowsWithNullHttpContent()
        {
            TestDataAssert.Execute(
                TestData.RepresentativeValueAndRefTypeTestDataCollection,
                (type, obj) =>
                {
                    ExceptionAssert.ThrowsArgumentNull(
                        "content",
                        () => new ObjectContent(type, (HttpContent)null));
                });
        }

        #endregion ObjectContent(Type, HttpContent)

        #region ObjectContent(Type, object, IEnumerable<MediaTypeFormatter>)

        [TestMethod, HostType("Moles")]
        [TestCategory("CIT"), Timeout(TimeoutConstant.DefaultTimeout), Owner("")]
        [Description("ObjectContent(Type, object, IEnumerable<MediaTypeFormatter>) sets Type, Formatters and (private) ObjectInstance properties.  ContentType defaults to null.")]
        public void Constructor4()
        {
            object ctorObject = null;
            MObjectContent.AllInstances.ValueSetObject = (@this, o) => ctorObject = o;

            TestDataAssert.Execute(
                TestData.RepresentativeValueAndRefTypeTestDataCollection,
                (type, obj) =>
                {
                    MediaTypeFormatter[] formatters = HttpTestData.StandardFormatters.ToArray();
                    ObjectContent content = new ObjectContent(type, obj, formatters);
                    Assert.AreSame(type, content.Type, "Failed to set Type");
                    Assert.IsNull(content.Headers.ContentType, "Content type should default to null.");
                    Assert.AreEqual(obj, ctorObject, "Failed to set ObjectInstance");
                    CollectionAssert.AreEqual(formatters, content.Formatters, "Formatters should have been same as passed in to ctor.");
                });
        }

        [Ignore]
        [TestMethod]
        [TestCategory("CIT"), Timeout(TimeoutConstant.DefaultTimeout), Owner("")]
        [Description("ObjectContent(Type, object, IEnumerable<MediaTypeFormatter>) uses standard formatters if none supplied.")]
        public void Constructor4SetsFormattersWithEmptyFormatterCollection()
        {
            IEnumerable<Type> standardFormatterTypes = HttpTestData.StandardFormatters.Select<MediaTypeFormatter,Type>((m) => m.GetType());

            TestDataAssert.Execute(
                TestData.RepresentativeValueAndRefTypeTestDataCollection,
                (type, obj) =>
                {
                    ObjectContent content = new ObjectContent(type, obj, new MediaTypeFormatter[0]);
                    List<Type> actualFormatterTypes = content.Formatters.Select<MediaTypeFormatter, Type>((m) => m.GetType()).ToList();
                    CollectionAssert.AreEqual(HttpTestData.StandardFormatterTypes.ToList(), actualFormatterTypes, "Formatter types should have been the standard ones.");
                });
        }

        [TestMethod]
        [TestCategory("CIT"), Timeout(TimeoutConstant.DefaultTimeout), Owner("")]
        [Description("ObjectContent(Type, object, IEnumerable<MediaTypeFormatter>) throws for a null Type parameter.")]
        public void Constructor4ThrowsWithNullType()
        {
            foreach (IEnumerable<MediaTypeFormatter> formatters in HttpTestData.AllFormatterCollections)
            {
                ExceptionAssert.ThrowsArgumentNull("type", () => new ObjectContent((Type)null, 5, formatters));
            }
        }

        [TestMethod]
        [TestCategory("CIT"), Timeout(TimeoutConstant.DefaultTimeout), Owner("")]
        [Description("ObjectContent(Type, object, IEnumerable<MediaTypeFormatter>) throws for null value type.")]
        public void Constructor4ThrowsWithNullValueTypeObject()
        {
            TestDataAssert.Execute(
                TestData.ValueTypeTestDataCollection,
                TestDataVariations.AsInstance,
                "Null value types should throw",
                (type, obj) =>
                {
                    ExceptionAssert.Throws<InvalidOperationException>(
                        SR.CannotUseNullValueType(typeof(ObjectContent).Name, type.Name),
                        () => new ObjectContent(type, (object)null, new MediaTypeHeaderValue("application/xml")));
                });
        }

        [TestMethod]
        [TestCategory("CIT"), Timeout(TimeoutConstant.DefaultTimeout), Owner("")]
        [Description("ObjectContent(Type, object, IEnumerable<MediaTypeFormatter>) throws for a null formatter list.")]
        public void Constructor4ThrowsWithNullFormatters()
        {
            ExceptionAssert.ThrowsArgumentNull(
                "formatters",
                () => new ObjectContent(typeof(int), 5, (IEnumerable<MediaTypeFormatter>)null));
        }


        [TestMethod]
        [TestCategory("CIT"), Timeout(TimeoutConstant.DefaultTimeout), Owner("")]
        [Description("ObjectContent(Type, object,  IEnumerable<MediaTypeFormatter>) throws if object is not assignable to Type.")]
        public void Constructor4ThrowsWithObjectNotAssignableToType()
        {
            TestDataAssert.Execute(
                TestData.RepresentativeValueAndRefTypeTestDataCollection,
                (type, obj) =>
                {
                    if (obj != null)
                    {
                        foreach (IEnumerable<MediaTypeFormatter> formatters in HttpTestData.AllFormatterCollections)
                        {
                            Type mismatchingType = (type == typeof(string)) ? typeof(int) : typeof(string);
                            ExceptionAssert.ThrowsArgument(
                                "value",
                                SR.ObjectAndTypeDisagree(obj.GetType().Name, mismatchingType.Name),
                                () => new ObjectContent(mismatchingType, obj, formatters));
                        }
                    }
                });
        }

        #endregion ObjectContent(Type, object, IEnumerable<MediaTypeFormatter>)

        #region ObjectContent(Type, object, string, IEnumerable<MediaTypeFormatter>)

        [TestMethod, HostType("Moles")]
        [TestCategory("CIT"), Timeout(TimeoutConstant.DefaultTimeout), Owner("")]
        [Description("ObjectContent(Type, object, string, IEnumerable<MediaTypeFormatter>) sets Type, content header's media type, Formatters and (private) ObjectInstance properties.")]
        public void Constructor5()
        {
            object ctorObject = null;
            MObjectContent.AllInstances.ValueSetObject = (@this, o) => ctorObject = o;

            TestDataAssert.Execute(
                TestData.RepresentativeValueAndRefTypeTestDataCollection,
                (type, obj) =>
                {
                    foreach (string mediaType in HttpTestData.LegalMediaTypeStrings)
                    {
                        foreach (IEnumerable<MediaTypeFormatter> formatters in HttpTestData.AllFormatterCollections)
                        {
                            ObjectContent content = new ObjectContent(type, obj, mediaType, formatters);
                            Assert.AreSame(type, content.Type, "Failed to set Type.");
                            Assert.AreEqual(obj, ctorObject, "Failed to set ObjectInstance.");
                            Assert.IsNotNull(content.Formatters, "Failed to set Formatters");
                            MediaTypeAssert.AreEqual(content.Headers.ContentType, mediaType, "MediaType was not set.");
                        }
                    };
                });
        }

        [TestMethod, HostType("Moles")]
        [TestCategory("CIT"), Timeout(TimeoutConstant.DefaultTimeout), Owner("")]
        [Description("ObjectContent(Type, object, string, IEnumerable<MediaTypeFormatter>) sets HttpContent property with object parameter==HttpContent.")]
        public void Constructor5SetsHttpContentWithHttpContentAsObject()
        {
            HttpContent ctorHttpContent = null;
            MObjectContent.AllInstances.HttpContentSetHttpContent = (@this, o) => ctorHttpContent = o;

            foreach (HttpContent httpContent in HttpTestData.StandardHttpContents)
            {
                foreach (string mediaType in HttpTestData.LegalMediaTypeStrings)
                {
                    foreach (IEnumerable<MediaTypeFormatter> formatters in HttpTestData.AllFormatterCollections)
                    {
                        ObjectContent content = new ObjectContent(typeof(string), (object)httpContent, mediaType, formatters);
                        Assert.AreEqual(httpContent, ctorHttpContent, "Failed to set HttpContent.");
                    }
                };
            };
        }

        [TestMethod, HostType("Moles")]
        [TestCategory("CIT"), Timeout(TimeoutConstant.DefaultTimeout), Owner("")]
        [Description("ObjectContent(Type, object, string, IEnumerable<MediaTypeFormatter>) sets HttpContent property to a StreamContent when Object parameter is a Stream.")]
        public void Constructor5SetsHttpContentWithStreamAsObject()
        {
            object ctorHttpContent = null;
            MObjectContent.AllInstances.HttpContentSetHttpContent = (@this, o) => ctorHttpContent = o;

            foreach (string mediaType in HttpTestData.LegalMediaTypeStrings)
            {
                foreach (IEnumerable<MediaTypeFormatter> formatters in HttpTestData.AllFormatterCollections)
                {
                    StreamAssert.UsingXmlSerializer<int>(
                        5,
                        (stream) =>
                        {
                            ObjectContent content = new ObjectContent(typeof(int), stream, mediaType, formatters);
                            StreamContent streamContent = ctorHttpContent as StreamContent;
                            Assert.IsNotNull(streamContent, "Stream was not wrapped in StreamContent.");
                            XmlSerializer serializer = new XmlSerializer(typeof(int));
                            int result = (int)serializer.Deserialize(streamContent.ContentReadStream);
                            Assert.AreEqual(5, result, "Expected stream to deserialize to this value.");
                        });
                }
            }
        }

        [TestMethod]
        [TestCategory("CIT"), Timeout(TimeoutConstant.DefaultTimeout), Owner("")]
        [Description("ObjectContent(Type, object, string, IEnumerable<MediaTypeFormatter>) throws for a null Type parameter.")]
        public void Constructor5ThrowsWithNullType()
        {
            foreach (string mediaType in HttpTestData.LegalMediaTypeStrings)
            {
                foreach (IEnumerable<MediaTypeFormatter> formatters in HttpTestData.AllFormatterCollections)
                {
                    ExceptionAssert.ThrowsArgumentNull("type", () => new ObjectContent((Type)null, 5, mediaType, formatters));
                }
            }
        }

        [TestMethod]
        [TestCategory("CIT"), Timeout(TimeoutConstant.DefaultTimeout), Owner("")]
        [Description("ObjectContent(Type, object, string, IEnumerable<MediaTypeFormatter>) throws for a null value type object.")]
        public void Constructor5ThrowsWithNullValueTypeObject()
        {
            TestDataAssert.Execute(
                TestData.ValueTypeTestDataCollection,
                TestDataVariations.AsInstance,
                "Null value types should throw",
                (type, obj) =>
                {
                    foreach (string mediaType in HttpTestData.LegalMediaTypeStrings)
                    {
                        foreach (IEnumerable<MediaTypeFormatter> formatters in HttpTestData.AllFormatterCollections)
                        {
                            ExceptionAssert.Throws<InvalidOperationException>(
                                SR.CannotUseNullValueType(typeof(ObjectContent).Name, type.Name),
                                () => new ObjectContent(type, (object)null, mediaType, formatters));
                        }
                    };
                });
        }

        [TestMethod]
        [TestCategory("CIT"), Timeout(TimeoutConstant.DefaultTimeout), Owner("")]
        [Description("ObjectContent(Type, object, string, IEnumerable<MediaTypeFormatter>) throws with an empty media type.")]
        public void Constructor5ThrowsWithEmptyMediaType()
        {
            TestDataAssert.Execute(
                TestData.RepresentativeValueAndRefTypeTestDataCollection,
                (type, obj) =>
                {
                    foreach (string mediaType in TestData.EmptyStrings)
                    {
                        foreach (IEnumerable<MediaTypeFormatter> formatters in HttpTestData.AllFormatterCollections)
                        {
                            ExceptionAssert.ThrowsArgumentNull(
                                "mediaType",
                                () => new ObjectContent(type, obj, mediaType, formatters));
                        }
                    };
                });
        }


        [TestMethod]
        [TestCategory("CIT"), Timeout(TimeoutConstant.DefaultTimeout), Owner("")]
        [Description("ObjectContent(Type, object, string,  IEnumerable<MediaTypeFormatter>) throws for an illegal media type.")]
        public void Constructor5ThrowsWithIllegalMediaType()
        {
            TestDataAssert.Execute(
            TestData.RepresentativeValueAndRefTypeTestDataCollection,
            (type, obj) =>
            {
                foreach (string mediaType in HttpTestData.IllegalMediaTypeStrings)
                {
                    foreach (IEnumerable<MediaTypeFormatter> formatters in HttpTestData.AllFormatterCollections)
                    {
                        ExceptionAssert.ThrowsArgument(
                            "mediaType",
                            SR.InvalidMediaType(mediaType, typeof(MediaTypeHeaderValue).Name),
                            () => new ObjectContent(type, obj, mediaType, formatters));
                    }
                };
            });
        }

        [TestMethod]
        [TestCategory("CIT"), Timeout(TimeoutConstant.DefaultTimeout), Owner("")]
        [Description("ObjectContent(Type, object, string, IEnumerable<MediaTypeFormatter>) throws with a null formatters.")]
        public void Constructor5ThrowsWithNullFormatters()
        {
            foreach (string mediaType in HttpTestData.LegalMediaTypeStrings)
            {
                ExceptionAssert.ThrowsArgumentNull(
                    "formatters",
                    () => new ObjectContent(typeof(int), 5, mediaType, (IEnumerable<MediaTypeFormatter>)null));
            }
        }

        [TestMethod]
        [TestCategory("CIT"), Timeout(TimeoutConstant.DefaultTimeout), Owner("")]
        [Description("ObjectContent(Type, object, string, IEnumerable<MediaTypeFormatter>) throws if object is not assignable to Type.")]
        public void Constructor5ThrowsWithObjectNotAssignableToType()
        {
            TestDataAssert.Execute(
                TestData.RepresentativeValueAndRefTypeTestDataCollection,
                (type, obj) =>
                {
                    if (obj != null)
                    {
                        foreach (string mediaType in HttpTestData.LegalMediaTypeStrings)
                        {
                            foreach (IEnumerable<MediaTypeFormatter> formatters in HttpTestData.AllFormatterCollections)
                            {
                                Type mismatchingType = (type == typeof(string)) ? typeof(int) : typeof(string);
                                ExceptionAssert.ThrowsArgument(
                                    "value",
                                    SR.ObjectAndTypeDisagree(obj.GetType().Name, mismatchingType.Name),
                                    () => new ObjectContent(mismatchingType, obj, mediaType, formatters));
                            }
                        }
                    }
                });
        }

        #endregion ObjectContent(Type, object, string, IEnumerable<MediaTypeFormatter>)

        #region ObjectContent(Type, object, MediaTypeHeaderValue, IEnumerable<MediaTypeFormatter>)

        [TestMethod, HostType("Moles")]
        [TestCategory("CIT"), Timeout(TimeoutConstant.DefaultTimeout), Owner("")]
        [Description("ObjectContent(Type, object, MediaTypeHeaderValue, IEnumerable<MediaTypeFormatter>) sets Type, content header's media type, Formatters and (private) ObjectInstance properties.")]
        public void Constructor6()
        {
            object ctorObject = null;
            MObjectContent.AllInstances.ValueSetObject = (@this, o) => ctorObject = o;

            TestDataAssert.Execute(
                TestData.RepresentativeValueAndRefTypeTestDataCollection,
                (type, obj) =>
                {
                    foreach (MediaTypeHeaderValue mediaType in HttpTestData.LegalMediaTypeHeaderValues)
                    {
                        foreach (IEnumerable<MediaTypeFormatter> formatters in HttpTestData.AllFormatterCollections)
                        {
                            ObjectContent content = new ObjectContent(type, obj, mediaType, formatters);
                            Assert.AreSame(type, content.Type, "Failed to set Type.");
                            Assert.AreEqual(obj, ctorObject, "Failed to set ObjectInstance.");
                            MediaTypeAssert.AreEqual(content.Headers.ContentType, mediaType, "MediaType was not set.");
                            Assert.IsNotNull(content.Formatters, "Failed to set Formatters");
                        }
                    };
                });
        }

        [TestMethod, HostType("Moles")]
        [TestCategory("CIT"), Timeout(TimeoutConstant.DefaultTimeout), Owner("")]
        [Description("ObjectContent(Type, object, MediaTypeHeaderValue, IEnumerable<MediaTypeFormatter>) sets HttpContent property with object parameter==HttpContent.")]
        public void Constructor6SetsHttpContentWithHttpContentAsObject()
        {
            HttpContent ctorHttpContent = null;
            MObjectContent.AllInstances.HttpContentSetHttpContent = (@this, o) => ctorHttpContent = o;

            foreach (HttpContent httpContent in HttpTestData.StandardHttpContents)
            {
                foreach (MediaTypeHeaderValue mediaType in HttpTestData.LegalMediaTypeHeaderValues)
                {
                    foreach (IEnumerable<MediaTypeFormatter> formatters in HttpTestData.AllFormatterCollections)
                    {
                        ObjectContent content = new ObjectContent(typeof(string), (object)httpContent, mediaType, formatters);
                        Assert.AreEqual(httpContent, ctorHttpContent, "Failed to set HttpContent.");
                    }
                };
            };
        }


        [TestMethod, HostType("Moles")]
        [TestCategory("CIT"), Timeout(TimeoutConstant.DefaultTimeout), Owner("")]
        [Description("ObjectContent(Type, object, MediaTypeHeaderValue, IEnumerable<MediaTypeFormatter>) sets HttpContent property to a StreamContent when Object parameter is a Stream.")]
        public void Constructor6SetsHttpContentWithStreamAsObject()
        {
            object ctorHttpContent = null;
            MObjectContent.AllInstances.HttpContentSetHttpContent = (@this, o) => ctorHttpContent = o;

            foreach (MediaTypeHeaderValue mediaType in HttpTestData.LegalMediaTypeHeaderValues)
            {
                foreach (IEnumerable<MediaTypeFormatter> formatters in HttpTestData.AllFormatterCollections)
                {
                    StreamAssert.UsingXmlSerializer<int>(
                        5,
                        (stream) =>
                        {
                            ObjectContent content = new ObjectContent(typeof(int), stream, mediaType, formatters);
                            StreamContent streamContent = ctorHttpContent as StreamContent;
                            Assert.IsNotNull(streamContent, "Stream was not wrapped in StreamContent.");
                            XmlSerializer serializer = new XmlSerializer(typeof(int));
                            int result = (int)serializer.Deserialize(streamContent.ContentReadStream);
                            Assert.AreEqual(5, result, "Expected stream to deserialize to this value.");
                        });
                }
            }
        }

        [TestMethod]
        [TestCategory("CIT"), Timeout(TimeoutConstant.DefaultTimeout), Owner("")]
        [Description("ObjectContent(Type, object, MediaTypeHeaderValue, IEnumerable<MediaTypeFormatter>) throws for a null Type parameter.")]
        public void Constructor6ThrowsWithNullType()
        {
            foreach (MediaTypeHeaderValue mediaType in HttpTestData.LegalMediaTypeHeaderValues)
            {
                foreach (IEnumerable<MediaTypeFormatter> formatters in HttpTestData.AllFormatterCollections)
                {
                    ExceptionAssert.ThrowsArgumentNull("type", () => new ObjectContent((Type)null, 5, mediaType, formatters));
                }
            }
        }

        [TestMethod]
        [TestCategory("CIT"), Timeout(TimeoutConstant.DefaultTimeout), Owner("")]
        [Description("ObjectContent(Type, object, MediaTypeHeaderValue, IEnumerable<MediaTypeFormatter>) throws for a null value type object.")]
        public void Constructor6ThrowsWithNullValueTypeObject()
        {
            TestDataAssert.Execute(
                TestData.ValueTypeTestDataCollection,
                TestDataVariations.AsInstance,
                "Null value types should throw",
                (type, obj) =>
                {
                    foreach (MediaTypeHeaderValue mediaType in HttpTestData.LegalMediaTypeHeaderValues)
                    {
                        foreach (IEnumerable<MediaTypeFormatter> formatters in HttpTestData.AllFormatterCollections)
                        {
                            ExceptionAssert.Throws<InvalidOperationException>(
                                SR.CannotUseNullValueType(typeof(ObjectContent).Name, type.Name),
                                () => new ObjectContent(type, (object)null, mediaType, formatters));
                        }
                    }
                });
        }

        [TestMethod]
        [TestCategory("CIT"), Timeout(TimeoutConstant.DefaultTimeout), Owner("")]
        [Description("ObjectContent(Type, object, MediaTypeHeaderValue, IEnumerable<MediaTypeFormatter>) throws with an null media type.")]
        public void Constructor6ThrowsWithNullMediaType()
        {
            foreach (IEnumerable<MediaTypeFormatter> formatters in HttpTestData.AllFormatterCollections)
            {
                ExceptionAssert.ThrowsArgumentNull(
                    "mediaType",
                    () => new ObjectContent(typeof(int), 5, (MediaTypeHeaderValue)null, formatters));
            }
        }

        [TestMethod]
        [TestCategory("CIT"), Timeout(TimeoutConstant.DefaultTimeout), Owner("")]
        [Description("ObjectContent(Type, object, MediaTypeHeaderValue, IEnumerable<MediaTypeFormatter>) throws with a null formatters.")]
        public void Constructor6ThrowsWithNullFormatters()
        {
            foreach (MediaTypeHeaderValue mediaType in HttpTestData.LegalMediaTypeHeaderValues)
            {
                ExceptionAssert.ThrowsArgumentNull(
                    "formatters",
                    () => new ObjectContent(typeof(int), 5, mediaType, (IEnumerable<MediaTypeFormatter>)null));
            }
        }

        [TestMethod]
        [TestCategory("CIT"), Timeout(TimeoutConstant.DefaultTimeout), Owner("")]
        [Description("ObjectContent(Type, object, MediaTypeHeaderValue, IEnumerable<MediaTypeFormatter>) throws if object is not assignable to Type.")]
        public void Constructor6ThrowsWithObjectNotAssignableToType()
        {
            TestDataAssert.Execute(
                TestData.RepresentativeValueAndRefTypeTestDataCollection,
                (type, obj) =>
                {
                    if (obj != null)
                    {
                        foreach (MediaTypeHeaderValue mediaType in HttpTestData.LegalMediaTypeHeaderValues)
                        {
                            foreach (IEnumerable<MediaTypeFormatter> formatters in HttpTestData.AllFormatterCollections)
                            {
                                Type mismatchingType = (type == typeof(string)) ? typeof(int) : typeof(string);
                                ExceptionAssert.ThrowsArgument(
                                    "value",
                                    SR.ObjectAndTypeDisagree(obj.GetType().Name, mismatchingType.Name),
                                    () => new ObjectContent(mismatchingType, obj, mediaType, formatters));
                            }
                        }
                    }
                });
        }

        #endregion ObjectContent(Type, object, MediaTypeHeaderValue, IEnumerable<MediaTypeFormatter>)

        #region ObjectContent(Type, HttpContent, IEnumerable<MediaTypeFormatter>)

        [TestMethod, HostType("Moles")]
        [TestCategory("CIT"), Timeout(TimeoutConstant.DefaultTimeout), Owner("")]
        [Description("ObjectContent(Type, HttpContent, IEnumerable<MediaTypeFormatter>) sets Type, HttpContent and Formatter properties, and sets MediaType from HttpContent.")]
        public void Constructor7()
        {
            object ctorHttpContent = null;
            MObjectContent.AllInstances.HttpContentSetHttpContent = (@this, o) => ctorHttpContent = o;

            TestDataAssert.Execute(
                TestData.RepresentativeValueAndRefTypeTestDataCollection,
                (type, obj) =>
                {
                    foreach (HttpContent httpContent in HttpTestData.StandardHttpContents)
                    {
                        foreach (IEnumerable<MediaTypeFormatter> formatters in HttpTestData.AllFormatterCollections)
                        {
                            ObjectContent content = new ObjectContent(type, httpContent);
                            Assert.AreSame(type, content.Type, "Failed to set Type.");
                            Assert.AreSame(httpContent, ctorHttpContent, "Failed to set HttpContent.");
                            Assert.IsNotNull(content.Formatters, "Failed to set Formatters.");
                            MediaTypeAssert.AreEqual(content.Headers.ContentType, httpContent.Headers.ContentType, "MediaType was not set.");
                        }
                    }
                });
        }

        [TestMethod]
        [TestCategory("CIT"), Timeout(TimeoutConstant.DefaultTimeout), Owner("")]
        [Description("ObjectContent(Type, HttpContent, IEnumerable<MediaTypeFormatter>) sets content headers from input HttpContent.")]
        public void Constructor7SetsContentHeadersWithHttpContent()
        {
            TestDataAssert.Execute(
                TestData.RepresentativeValueAndRefTypeTestDataCollection,
                (type, obj) =>
                {
                    foreach (HttpContent httpContent in HttpTestData.StandardHttpContents)
                    {
                        foreach (IEnumerable<MediaTypeFormatter> formatters in HttpTestData.AllFormatterCollections)
                        {
                            httpContent.Headers.Add("CIT-Name", "CIT-Value");
                            ObjectContent content = new ObjectContent(type, httpContent);
                            HttpAssert.Contains(content.Headers, "CIT-Name", "CIT-Value");
                        }
                    }
                });
        }

        [TestMethod]
        [TestCategory("CIT"), Timeout(TimeoutConstant.DefaultTimeout), Owner("")]
        [Description("ObjectContent(Type, HttpContent, IEnumerable<MediaTypeFormatter>) throws for a null Type parameter.")]
        public void Constructor7ThrowsWithNullType()
        {
            foreach (HttpContent httpContent in HttpTestData.StandardHttpContents)
            {
                foreach (IEnumerable<MediaTypeFormatter> formatters in HttpTestData.AllFormatterCollections)
                {
                    ExceptionAssert.ThrowsArgumentNull("type", () => new ObjectContent((Type)null, httpContent, formatters));
                }
            }
        }

        [TestMethod]
        [TestCategory("CIT"), Timeout(TimeoutConstant.DefaultTimeout), Owner("")]
        [Description("ObjectContent(Type, HttpContent, IEnumerable<MediaTypeFormatter>) throws for a null HttpContent.")]
        public void Constructor7ThrowsWithNullHttpContent()
        {
            TestDataAssert.Execute(
                TestData.RepresentativeValueAndRefTypeTestDataCollection,
                (type, obj) =>
                {
                    foreach (IEnumerable<MediaTypeFormatter> formatters in HttpTestData.AllFormatterCollections)
                    {
                        ExceptionAssert.ThrowsArgumentNull(
                            "content",
                            () => new ObjectContent(type, (HttpContent)null, formatters));
                    }
                });
        }

        [TestMethod]
        [TestCategory("CIT"), Timeout(TimeoutConstant.DefaultTimeout), Owner("")]
        [Description("ObjectContent(Type, HttpContent, IEnumerable<MediaTypeFormatter>) throws for a null formatters.")]
        public void Constructor7ThrowsWithNullFormatters()
        {
            foreach (HttpContent httpContent in HttpTestData.StandardHttpContents)
            {
                ExceptionAssert.ThrowsArgumentNull(
                    "formatters",
                    () => new ObjectContent(typeof(int), httpContent, (IEnumerable<MediaTypeFormatter>)null));
            }
        }

        #endregion ObjectContent(Type, HttpContent, IEnumerable<MediaTypeFormatter>)

        #endregion Constructors

        #region Properties

        #region Formatters

        [TestMethod]
        [TestCategory("CIT"), Timeout(TimeoutConstant.DefaultTimeout), Owner("")]
        [Description("Formatters property returns mutable MediaTypeFormatter collection.")]
        public void FormattersReturnsMutableCollection()
        {
            ObjectContent content = new ObjectContent(typeof(string), "data");
            MediaTypeFormatterCollection collection = content.Formatters;
            Assert.IsNotNull(collection, "Formatters cannot be null.");

            SMediaTypeFormatter formatter = new SMediaTypeFormatter();
            collection.Add(formatter);
            CollectionAssert.Contains(collection, formatter, "Collection should contain formatter we added.");
        }

        #endregion Formatters

        #region HttpRequestMessage (internal)

        [TestMethod]
        [TestCategory("CIT"), Timeout(TimeoutConstant.DefaultTimeout), Owner("")]
        [Description("HttpRequestMessage internal property returns HttpRequestMessage if they are pairs.")]
        public void HttpRequestMessageGetsValueWithPairing()
        {
            ObjectContent content = new ObjectContent(typeof(string), "data");
            HttpRequestMessage request = new HttpRequestMessage();
            request.Content = content;
            content.HttpRequestMessage = request;
            Assert.AreSame(request, content.HttpRequestMessage, "HttpRequestMessage property was not set.");
        }

        [TestMethod]
        [TestCategory("CIT"), Timeout(TimeoutConstant.DefaultTimeout), Owner("")]
        [Description("HttpRequestMessage internal property returns null if not paired with HttpRequestMessage.")]
        public void HttpRequestMessageGetsNullWithNoPairing()
        {
            ObjectContent content = new ObjectContent(typeof(string), "data");
            HttpRequestMessage request = new HttpRequestMessage();
            content.HttpRequestMessage = request;
            Assert.IsNull(content.HttpRequestMessage, "HttpRequestMessage should be null if not paired.");
        }

        [TestMethod]
        [TestCategory("CIT"), Timeout(TimeoutConstant.DefaultTimeout), Owner("")]
        [Description("HttpRequestMessage internal property returns a newly paired HttpRequestMessage.")]
        public void HttpRequestMessageGetsAlteredPairing()
        {
            ObjectContent content1 = new ObjectContent(typeof(string), "data");
            ObjectContent content2 = new ObjectContent(typeof(int), 5);
            HttpRequestMessage request = new HttpRequestMessage();

            // Pair and verify
            request.Content = content1;
            content1.HttpRequestMessage = request;
            Assert.AreSame(request, content1.HttpRequestMessage, "HttpRequestMessage property should have been its pair.");

            // Alter half of the pairing and verify the original pairing is gone
            request.Content = content2;
            Assert.IsNull(content1.HttpRequestMessage, "HttpRequestMessage should be null if altered pairing.");

            // Complete the other half of the pairing and verify it is intact
            content2.HttpRequestMessage = request;
            Assert.AreSame(request, content2.HttpRequestMessage, "HttpRequestMessage property should have been its pair.");
        }

        [TestMethod]
        [TestCategory("CIT"), Timeout(TimeoutConstant.DefaultTimeout), Owner("")]
        [Description("HttpRequestMessage property setter clears the HttpResponseMessage property.")]
        public void HttpRequestMessageSetsHttpResponseMessageToNull()
        {
            ObjectContent content = new ObjectContent(typeof(string), "data");
            HttpResponseMessage response = new HttpResponseMessage();
            content.HttpResponseMessage = response;
            response.Content = content;
            Assert.AreSame(response, content.HttpResponseMessage, "HttpResponseMessage should have been paired with content.");

            // Now repair the content with a requst
            HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Get, "http:/somehost");
            content.HttpRequestMessage = request;
            request.Content = content;

            Assert.AreSame(request, content.HttpRequestMessage, "HttpRequestMessage should have re-paired with content.");
            Assert.IsNull(content.HttpResponseMessage, "HttpResponseMessage should be null after setting HttpRequestMessage.");
        }

        #endregion HttpRequestMessage (internal)

        #region HttpResponseMessage (internal)

        [TestMethod]
        [TestCategory("CIT"), Timeout(TimeoutConstant.DefaultTimeout), Owner("")]
        [Description("HttpResponseMessage property returns HttpResponseMessage if they are pairs.")]
        public void HttpResponseMessageGetsValueWithPairing()
        {
            ObjectContent content = new ObjectContent(typeof(string), "data");
            HttpResponseMessage response = new HttpResponseMessage();
            response.Content = content;
            content.HttpResponseMessage = response;
            Assert.AreSame(response, content.HttpResponseMessage, "HttpResponseMessage property was not set.");
        }

        [TestMethod]
        [TestCategory("CIT"), Timeout(TimeoutConstant.DefaultTimeout), Owner("")]
        [Description("HttpResponseMessage property returns null if not paired with HttpResponseMessage.")]
        public void HttpResponseMessageGetsNullWithNoPairing()
        {
            ObjectContent content = new ObjectContent(typeof(string), "data");
            HttpResponseMessage response = new HttpResponseMessage();
            content.HttpResponseMessage = response;
            Assert.IsNull(content.HttpResponseMessage, "HttpResponseMessage should be null if not paired.");
        }

        [TestMethod]
        [TestCategory("CIT"), Timeout(TimeoutConstant.DefaultTimeout), Owner("")]
        [Description("HttpResponseMessage property returns a newly paired HttpResponseMessage.")]
        public void HttpResponseMessageGetsAlteredPairing()
        {
            ObjectContent content1 = new ObjectContent(typeof(string), "data");
            ObjectContent content2 = new ObjectContent(typeof(int), 5);
            HttpResponseMessage response = new HttpResponseMessage();

            // Pair and verify
            response.Content = content1;
            content1.HttpResponseMessage = response;
            Assert.AreSame(response, content1.HttpResponseMessage, "HttpResponseMessage property should have been its pair.");

            // Alter half of the pairing and verify the original pairing is gone
            response.Content = content2;
            Assert.IsNull(content1.HttpResponseMessage, "HttpResponseMessage should be null if altered pairing.");

            // Complete the other half of the pairing and verify it is intact
            content2.HttpResponseMessage = response;
            Assert.AreSame(response, content2.HttpResponseMessage, "HttpResponseMessage property should have been its pair.");
        }

        [TestMethod]
        [TestCategory("CIT"), Timeout(TimeoutConstant.DefaultTimeout), Owner("")]
        [Description("HttpResponseMessage property setter clears the HttpRequestMessage property.")]
        public void HttpResponseMessageSetsHttpRequestMessageToNull()
        {
            ObjectContent content = new ObjectContent(typeof(string), "data");
            HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Get, "http:/somehost");

            content.HttpRequestMessage = request;
            request.Content = content;
            Assert.AreSame(request, content.HttpRequestMessage, "HttpRequestMessage should have been paired with content.");

            // Now repair the content with a response
            HttpResponseMessage response = new HttpResponseMessage();
            content.HttpResponseMessage = response;
            response.Content = content;
            Assert.AreSame(response, content.HttpResponseMessage, "HttpResponseMessage should have re-paired with content.");
            Assert.IsNull(content.HttpRequestMessage, "HttpRequestMessage should be null after setting HttpResponseMessage.");
        }

        #endregion HttpResponseMessage (internal)

        #endregion Properties

        #region Methods

        #region DetermineWriteSerializerAndContentType()

        [TestMethod, HostType("Moles")]
        [TestCategory("CIT"), Timeout(TimeoutConstant.DefaultTimeout), Owner("")]
        [Description("DetermineWriteSerializerAndContentType() internal method selects formatter and content type.")]
        public void DetermineWriteSerializerAndContentType()
        {
            ObjectContent content = new ObjectContent(typeof(int), 5);
            Assert.IsNull(content.Headers.ContentType, "ContentType should have initialized to null.");
            content.DetermineWriteSerializerAndContentType();
            MediaTypeAssert.AreEqual(XmlMediaTypeFormatter.DefaultMediaType, content.Headers.ContentType, "Should have selected XmlMediaTypeFormatter's content type.");
        }

        #endregion DetermineWriteSerializerAndContentType()

        #region CopyTo()

        [TestMethod, HostType("Moles")]
        [TestCategory("CIT"), Timeout(TimeoutConstant.DefaultTimeout), Owner("")]
        [Description("CopyTo(Stream) public method calls SerializeToStream protected method.")]
        public void CopyToCallsSerializeToStream()
        {
            SObjectContent stubContent = new SObjectContent(typeof(int), 5) { CallBase = true };
            MObjectContent moleContent = new MObjectContent(stubContent);
            bool serializeToStreamCalled = false;
            ObjectContent content = (ObjectContent)moleContent;
            moleContent.SerializeToStreamStreamTransportContext = (stream, transportContext) => serializeToStreamCalled = true;
            using (MemoryStream stream = new MemoryStream())
            {
                content.CopyTo(stream);
            }

            Assert.IsTrue(serializeToStreamCalled, "CopyTo did not call SerializeToStream");
        }

        #endregion CopyTo()

        #region CopyToAsync()

        [TestMethod, HostType("Moles")]
        [TestCategory("CIT"), Timeout(TimeoutConstant.DefaultTimeout), Owner("")]
        [Description("CopyToAsync(Stream) public method calls SerializeToStreamAsync protected method.")]
        public void CopyToAsyncCallsSerializeToStreamAsync()
        {
            SObjectContent stubContent = new SObjectContent(typeof(int), 5) { CallBase = true };
            MObjectContent moleContent = new MObjectContent(stubContent);
            bool serializeToStreamCalled = false;
            bool taskRan = false;
            ObjectContent content = (ObjectContent)moleContent;

            moleContent.SerializeToStreamAsyncStreamTransportContext = (stream, transportContext) =>
            {
                serializeToStreamCalled = true;
                return Task.Factory.StartNew(() => { taskRan = true;  });
            };

            using (MemoryStream stream = new MemoryStream())
            {
                Task readTask = content.CopyToAsync(stream);
                TaskAssert.Succeeds(readTask);
            }

            Assert.IsTrue(serializeToStreamCalled, "CopyToAsync did not call SerializeToStreamAsync.");
            Assert.IsTrue(taskRan, "CopyToAsync ran the Task returned.");
        }

        #endregion CopyToAsync()

        #region SerializeToStream()

        [TestMethod, HostType("Moles")]
        [TestCategory("CIT"), Timeout(TimeoutConstant.DefaultTimeout), Owner("")]
        [Description("SerializeToStream() calls the registered MediaTypeFormatter for SupportedMediaTypes and OnWriteToStream.")]
        public void SerializeToStreamCallsFormatter()
        {
            SStringContent content = new SStringContent("data");
            Collection<MediaTypeHeaderValue> mediaTypeCollection = new Collection<MediaTypeHeaderValue>() { content.Headers.ContentType };

            bool askedForSupportedMediaTypes = false;
            bool onWriteToStreamCalled = false;
            SMediaTypeFormatter stubFormatter = new SMediaTypeFormatter();
            stubFormatter.OnCanWriteTypeType = (type) => true;
            stubFormatter.OnWriteToStreamTypeObjectStreamHttpContentHeadersTransportContext = (type, obj, stream, headers, context) => onWriteToStreamCalled = true;

            MMediaTypeFormatter formatter = new MMediaTypeFormatter(stubFormatter);
            formatter.SupportedMediaTypesGet = () => { askedForSupportedMediaTypes = true; return mediaTypeCollection; };

            ObjectContent objectContent = new ObjectContent(typeof(string), content);
            MediaTypeFormatterCollection formatterCollection = objectContent.Formatters;
            formatterCollection.Clear();
            formatterCollection.Add(formatter);

            // This statement should call CanReadType, get SupportedMediaTypes, discover the formatter and call it

            StreamAssert.WriteAndRead(
                (stream) => objectContent.CopyTo(stream),
                (stream) => { }
                );

            Assert.IsTrue(askedForSupportedMediaTypes, "SerializeToStream did not ask for supported media types.");
            Assert.IsTrue(onWriteToStreamCalled, "SerializeToStream did not call our formatter.");
        }

        [TestMethod]
        [TestCategory("CIT"), Timeout(TimeoutConstant.DefaultTimeout), Owner("")]
        [Description("SerializeToStream() uses the XmlMediaTypeFormatter if no matching formatters are available.")]
        public void SerializeToStreamUsesXmlMediaTypeFormatterWithNoMatchingFormatters()
        {
            ObjectContent content = new ObjectContent(typeof(int), 5, new MediaTypeHeaderValue("application/unknown"));
            int returnedValue = StreamAssert.WriteAndReadResult<int>(
                (stream) => content.CopyTo(stream),
                (stream) =>
                {
                    XmlSerializer xmlSerializer = new XmlSerializer(typeof(int));
                    object result = xmlSerializer.Deserialize(stream);
                    Assert.IsNotNull(result, "XmlSerializer returned null result.");
                    return (int)result;
                });

            Assert.AreEqual(5, returnedValue, "XmlSerializer returned wrong value");
        }

        #endregion SerializeToStream()

        #region SerializeToStreamAsync()

        [TestMethod]
        [TestCategory("CIT"), Timeout(TimeoutConstant.DefaultTimeout), Owner("")]
        [Description("SerializeToStreamAsync() uses the XmlMediaTypeFormatter if no matching formatters are available.")]
        public void SerializeToStreamAsyncUsesXmlMediaTypeFormatterWithNoMatchingFormatters()
        {
            ObjectContent content = new ObjectContent(typeof(int), 5, new MediaTypeHeaderValue("application/unknown"));
            int returnedValue = StreamAssert.WriteAndReadResult<int>(
                (stream) => TaskAssert.Succeeds(content.CopyToAsync(stream)),
                (stream) =>
                {
                    XmlSerializer xmlSerializer = new XmlSerializer(typeof(int));
                    object result = xmlSerializer.Deserialize(stream);
                    Assert.IsNotNull(result, "XmlSerializer returned null result.");
                    return (int)result;
                });

            Assert.AreEqual(5, returnedValue, "XmlSerializer returned wrong value");
        }

        [TestMethod, HostType("Moles")]
        [TestCategory("CIT"), Timeout(TimeoutConstant.DefaultTimeout), Owner("")]
        [Description("SerializeToStreamAsync() calls the registered MediaTypeFormatter for SupportedMediaTypes and OnWriteToStream.")]
        public void SerializeToStreamAsyncCallsFormatter()
        {
            MediaTypeHeaderValue customMediaType = new MediaTypeHeaderValue("application/mine");
            Collection<MediaTypeHeaderValue> mediaTypeCollection = new Collection<MediaTypeHeaderValue>() { customMediaType };
            bool askedForSupportedMediaTypes = false;
            bool onWriteToStreamCalled = false;
            SMediaTypeFormatter stubFormatter = new SMediaTypeFormatter();
            stubFormatter.OnCanWriteTypeType = (type) => true;
            stubFormatter.OnWriteToStreamAsyncTypeObjectStreamHttpContentHeadersTransportContext =
                (type, obj, stream, headers, context) =>
                {
                    onWriteToStreamCalled = true;
                    return Task.Factory.StartNew(() => { });
                };

            MMediaTypeFormatter formatter = new MMediaTypeFormatter(stubFormatter);
            formatter.SupportedMediaTypesGet = () => { askedForSupportedMediaTypes = true; return mediaTypeCollection; };

            ObjectContent objectContent = new ObjectContent(typeof(string), "data");
            objectContent.Headers.ContentType = customMediaType;
            MediaTypeFormatterCollection formatterCollection = objectContent.Formatters;
            formatterCollection.Clear();
            formatterCollection.Add(formatter);

            // This statement should call CanWriteType, get SupportedMediaTypes, discover the formatter and call it
            Task writeTask = null;

            // Wait for task to complete before StreamAssert disposes it
            StreamAssert.WriteAndRead(
                (stream) => writeTask = objectContent.CopyToAsync(stream),
                (stream) => { TaskAssert.Succeeds(writeTask); }
                );

            Assert.IsTrue(askedForSupportedMediaTypes, "SerializeToStream did not ask for supported media types.");
            Assert.IsTrue(onWriteToStreamCalled, "SerializeToStream did not call our formatter.");
        }


        #endregion SerializeToStreamAsync()

        #region ReadAs()

        [TestMethod]
        [TestCategory("CIT"), Timeout(TimeoutConstant.DefaultTimeout), Owner("")]
        [Description("ReadAs() gets the object instance provided to the constructor for all value and reference types.")]
        public void ReadAsGetsObject()
        {
            TestDataAssert.Execute(
                TestData.RepresentativeValueAndRefTypeTestDataCollection,
                (type, obj) =>
                {
                    ObjectContent content = new ObjectContent(type, obj);
                    object readObj = content.ReadAs();
                    Assert.AreEqual(obj, readObj, string.Format("ReadAs failed for type '{0}'.", type.Name));
                });
        }

        [TestMethod]
        [TestCategory("CIT"), Timeout(TimeoutConstant.DefaultTimeout), Owner("")]
        [Description("ReadAs() throws for all value and reference types if no formatter is available.")]
        public void ReadAsThrowsWithNoFormatter()
        {
            TestDataAssert.Execute(
                TestData.RepresentativeValueAndRefTypeTestDataCollection,
                (type, obj) =>
                {
                    if (!HttpTestData.IsKnownUnserializable(type, obj) && HttpTestData.CanRoundTrip(type))
                    {
                        StreamAssert.UsingXmlSerializer(
                            type,
                            obj,
                            (stream) =>
                            {
                                StreamContent streamContent = new StreamContent(stream);
                                streamContent.Headers.ContentType = new MediaTypeHeaderValue("application/unknownMediaType");
                                ObjectContent contentWrappingStream = new ObjectContent(type, streamContent);
                                string errorMessage = SR.NoReadSerializerAvailable(typeof(MediaTypeFormatter).Name, type.Name, "application/unknownMediaType");
                                ExceptionAssert.Throws<InvalidOperationException>(
                                    "No formatters should throw.",
                                    errorMessage,
                                    () => contentWrappingStream.ReadAs());
                            });
                    }
                });
        }

        [TestMethod]
        [TestCategory("CIT"), Timeout(TimeoutConstant.DefaultTimeout), Owner("")]
        [Description("ReadAs() from an Xml Stream produced by ObjectContent.CopyTo() round-trips correctly.")]
        public void ReadAsFromStreamGetsRoundTrippedObject()
        {
            TestDataAssert.Execute(
                TestData.RepresentativeValueAndRefTypeTestDataCollection,
                (type, obj) =>
                {
                    if (!HttpTestData.IsKnownUnserializable(type, obj) && HttpTestData.CanRoundTrip(type))
                    {
                        ObjectContent content = new ObjectContent(type, obj, XmlMediaTypeFormatter.DefaultMediaType);
                        StreamAssert.WriteAndRead(
                            (stream) => content.CopyTo(stream),
                            (stream) =>
                            {
                                StreamContent streamContent = new StreamContent(stream);
                                streamContent.Headers.ContentType = content.Headers.ContentType;
                                ObjectContent contentWrappingStream = new ObjectContent(type, streamContent);
                                object readObj = contentWrappingStream.ReadAs();
                                TestDataAssert.AreEqual(obj, readObj, "Failed to round trip.");
                            });
                    }
                });
        }

        [TestMethod, HostType("Moles")]
        [TestCategory("CIT"), Timeout(TimeoutConstant.DefaultTimeout), Owner("")]
        [Description("ReadAs() calls the registered MediaTypeFormatter for SupportedMediaTypes and ReadFromStream.")]
        public void ReadAsCallsFormatter()
        {
            SStringContent content = new SStringContent("data");
            Collection<MediaTypeHeaderValue> mediaTypeCollection = new Collection<MediaTypeHeaderValue>() { content.Headers.ContentType };

            bool askedForSupportedMediaTypes = false;
            SMediaTypeFormatter stubFormatter = new SMediaTypeFormatter();
            stubFormatter.OnCanReadTypeType = (type) => true;
  
            MMediaTypeFormatter formatter = new MMediaTypeFormatter(stubFormatter);
            formatter.SupportedMediaTypesGet = () => { askedForSupportedMediaTypes = true; return mediaTypeCollection; };
            formatter.ReadFromStreamTypeStreamHttpContentHeaders = (type, stream, headers) => "mole data";

            ObjectContent objectContent = new ObjectContent(typeof(string), content);
            MediaTypeFormatterCollection formatterCollection = objectContent.Formatters;
            formatterCollection.Clear();
            formatterCollection.Add(formatter);

            // This statement should call CanReadType, get SupportedMediaTypes, discover the formatter and call it
            object readObj = objectContent.ReadAs();

            Assert.IsTrue(askedForSupportedMediaTypes, "ReadAs did not ask for supported media types.");
            Assert.AreEqual("mole data", readObj, "ReadAs did not return what the formatter returned.");
        }

        [TestMethod, HostType("Moles")]
        [TestCategory("CIT"), Timeout(TimeoutConstant.DefaultTimeout), Owner("")]
        [Description("ReadAs() calls the registered MediaTypeFormatter for SupportedMediaTypes and ReadFromStream only once, and then uses the cached value.")]
        public void ReadAsCallsFormatterOnceOnly()
        {
            SStringContent stubContent = new SStringContent("data") { CallBase = true };
            MHttpContent moleContent = new MHttpContent(stubContent);
            bool contentWasDisposed = false;
            moleContent.Dispose = () => contentWasDisposed = true;
            HttpContent httpContent = (HttpContent) moleContent;

            Collection<MediaTypeHeaderValue> mediaTypeCollection = new Collection<MediaTypeHeaderValue>() { httpContent.Headers.ContentType };
            
            bool askedForSupportedMediaTypes = false;
            SMediaTypeFormatter stubFormatter = new SMediaTypeFormatter();
            stubFormatter.OnCanReadTypeType = (type) => true;

            MMediaTypeFormatter formatter = new MMediaTypeFormatter(stubFormatter);
            formatter.SupportedMediaTypesGet = () => { askedForSupportedMediaTypes = true; return mediaTypeCollection; };
            formatter.ReadFromStreamTypeStreamHttpContentHeaders = (type, stream, headers) => "mole data";

            ObjectContent objectContent = new ObjectContent(typeof(string), httpContent);
            MediaTypeFormatterCollection formatterCollection = objectContent.Formatters;
            formatterCollection.Clear();
            formatterCollection.Add(formatter);

            // This statement should call CanReadType, get SupportedMediaTypes, discover the formatter and call it
            object readObj = objectContent.ReadAs();

            Assert.IsTrue(askedForSupportedMediaTypes, "ReadAs did not ask for supported media types.");
            Assert.AreEqual("mole data", readObj, "ReadAs did not return what the formatter returned.");

            // 1st ReadAs should have cached the Value and disposed the wrapped HttpContent
            Assert.IsTrue(contentWasDisposed, "1st ReadAs should have disposed the wrapped HttpContent.");

            // --- 2nd ReadAs should use cached value and not interact with formatter ---
            formatter.SupportedMediaTypesGet = () =>
            {
                Assert.Fail("2nd read should not ask formatter for supported media types.");
                return null;
            };

            formatter.ReadFromStreamTypeStreamHttpContentHeaders = (type, stream, headers) =>
            {
                Assert.Fail("2nd read should not call formatter.ReadFromStream.");
                return null;
            };

            readObj = objectContent.ReadAs();
            Assert.AreEqual("mole data", readObj, "ReadAs did not return the cached value.");
        }

        #endregion ReadAs()

        #region ReadAsAsync()

        [TestMethod]
        [TestCategory("CIT"), Timeout(TimeoutConstant.DefaultTimeout), Owner("")]
        [Description("ReadAsAsync() gets a Task<object> returning object instance provided to the constructor for all value and reference types.")]
        public void ReadAsAsyncGetsTaskOfObject()
        {
            TestDataAssert.Execute(
                TestData.RepresentativeValueAndRefTypeTestDataCollection,
                (type, obj) =>
                {
                    ObjectContent content = new ObjectContent(type, obj);
                    Task<object> task = content.ReadAsAsync();
                    TaskAssert.ResultEquals(task, obj);
                });
        }

        [TestMethod]
        [TestCategory("CIT"), Timeout(TimeoutConstant.DefaultTimeout), Owner("")]
        [Description("ReadAsAsync() throws for all value and reference types if no formatter is available.")]
        public void ReadAsAsyncThrowsWithNoFormatter()
        {
            TestDataAssert.Execute(
                TestData.RepresentativeValueAndRefTypeTestDataCollection,
                (type, obj) =>
                {
                    if (!HttpTestData.IsKnownUnserializable(type, obj) && HttpTestData.CanRoundTrip(type))
                    {
                        StreamAssert.UsingXmlSerializer(
                            type,
                            obj,
                            (stream) =>
                            {
                                StreamContent streamContent = new StreamContent(stream);
                                streamContent.Headers.ContentType = new MediaTypeHeaderValue("application/unknownMediaType");
                                ObjectContent contentWrappingStream = new ObjectContent(type, streamContent);
                                string errorMessage = SR.NoReadSerializerAvailable(typeof(MediaTypeFormatter).Name, type.Name, "application/unknownMediaType");
                                ExceptionAssert.Throws<InvalidOperationException>(
                                    "No formatters should throw.",
                                    errorMessage,
                                    () => contentWrappingStream.ReadAsAsync());
                            });
                    }
                });
        }

        [TestMethod]
        [TestCategory("CIT"), Timeout(TimeoutConstant.DefaultTimeout), Owner("")]
        [Description("ReadAsAsync() from an Xml Stream produced by ObjectContent.CopyToAsync() round-trips correctly.")]
        public void ReadAsAsyncFromStreamGetsRoundTrippedObject()
        {
            TestDataAssert.Execute(
                TestData.RepresentativeValueAndRefTypeTestDataCollection,
                (type, obj) =>
                {
                    if (!HttpTestData.IsKnownUnserializable(type, obj) && HttpTestData.CanRoundTrip(type))
                    {
                        ObjectContent content = new ObjectContent(type, obj, XmlMediaTypeFormatter.DefaultMediaType);
                        StreamAssert.WriteAndRead(
                            (stream) => TaskAssert.Succeeds(content.CopyToAsync(stream)),
                            (stream) =>
                            {
                                StreamContent streamContent = new StreamContent(stream);
                                streamContent.Headers.ContentType = content.Headers.ContentType;
                                ObjectContent contentWrappingStream = new ObjectContent(type, streamContent);
                                object readObj = TaskAssert.SucceedsWithResult(contentWrappingStream.ReadAsAsync());
                                TestDataAssert.AreEqual(obj, readObj, "Failed to round trip.");
                            });
                    }
                });
        }

        [TestMethod, HostType("Moles")]
        [TestCategory("CIT"), Timeout(TimeoutConstant.DefaultTimeout), Owner("")]
        [Description("ReadAsAsync() calls the registered MediaTypeFormatter for SupportedMediaTypes and ReadFromStream.")]
        public void ReadAsAsyncCallsFormatter()
        {
            SStringContent content = new SStringContent("data");
            Collection<MediaTypeHeaderValue> mediaTypeCollection = new Collection<MediaTypeHeaderValue>() { content.Headers.ContentType };

            bool askedForSupportedMediaTypes = false;
            SMediaTypeFormatter stubFormatter = new SMediaTypeFormatter();
            stubFormatter.OnCanReadTypeType = (type) => true;

            MMediaTypeFormatter formatter = new MMediaTypeFormatter(stubFormatter);
            formatter.SupportedMediaTypesGet = () => { askedForSupportedMediaTypes = true; return mediaTypeCollection; };
            formatter.ReadFromStreamAsyncTypeStreamHttpContentHeaders = (type, stream, headers) => Task.Factory.StartNew<object>(() => "mole data");

            ObjectContent objectContent = new ObjectContent(typeof(string), content);
            MediaTypeFormatterCollection formatterCollection = objectContent.Formatters;
            formatterCollection.Clear();
            formatterCollection.Add(formatter);

            // This statement should call CanReadType, get SupportedMediaTypes, discover the formatter and call it
            Task<object> readTask = objectContent.ReadAsAsync();
            object readObj = TaskAssert.SucceedsWithResult(readTask);

            Assert.IsTrue(askedForSupportedMediaTypes, "ReadAsAsync did not ask for supported media types.");
            Assert.AreEqual("mole data", readObj, "ReadAsAsync did not return what the formatter returned.");
        }

        [TestMethod, HostType("Moles")]
        [TestCategory("CIT"), Timeout(TimeoutConstant.DefaultTimeout), Owner("")]
        [Description("ReadAsAsync() calls the registered MediaTypeFormatter for SupportedMediaTypes and ReadFromStream only once and then uses the cached value.")]
        public void ReadAsAsyncCallsFormatterOnceOnly()
        {
            SStringContent stubContent = new SStringContent("data") { CallBase = true };
            MHttpContent moleContent = new MHttpContent(stubContent);
            bool contentWasDisposed = false;
            moleContent.Dispose = () => contentWasDisposed = true;
            HttpContent httpContent = (HttpContent)moleContent;

            Collection<MediaTypeHeaderValue> mediaTypeCollection = new Collection<MediaTypeHeaderValue>() { httpContent.Headers.ContentType };

            bool askedForSupportedMediaTypes = false;
            SMediaTypeFormatter stubFormatter = new SMediaTypeFormatter();
            stubFormatter.OnCanReadTypeType = (type) => true;

            MMediaTypeFormatter formatter = new MMediaTypeFormatter(stubFormatter);
            formatter.SupportedMediaTypesGet = () => { askedForSupportedMediaTypes = true; return mediaTypeCollection; };
            formatter.ReadFromStreamAsyncTypeStreamHttpContentHeaders = (type, stream, headers) => Task.Factory.StartNew<object>(() => "mole data");

            ObjectContent objectContent = new ObjectContent(typeof(string), httpContent);
            MediaTypeFormatterCollection formatterCollection = objectContent.Formatters;
            formatterCollection.Clear();
            formatterCollection.Add(formatter);

            // This statement should call CanReadType, get SupportedMediaTypes, discover the formatter and call it
            Task<object> readTask = objectContent.ReadAsAsync();
            object readObj = TaskAssert.SucceedsWithResult(readTask);

            Assert.IsTrue(askedForSupportedMediaTypes, "ReadAsAsync did not ask for supported media types.");
            Assert.AreEqual("mole data", readObj, "ReadAsAsync did not return what the formatter returned.");

            // 1st ReadAs should have cached the Value and disposed the wrapped HttpContent
            Assert.IsTrue(contentWasDisposed, "1st ReadAsAsync should have disposed the wrapped HttpContent.");

            // --- A 2nd call to ReadAsAsync should no longer interact with the formatter ---
            formatter.SupportedMediaTypesGet = () => 
            { 
                Assert.Fail("2nd read should not ask formatter for supported media types."); 
                return null; 
            };

            formatter.ReadFromStreamAsyncTypeStreamHttpContentHeaders = (type, stream, headers) =>
            {
                Assert.Fail("2nd read should not call formatter.ReadFromStreamAsync.");
                return null;
            };

            readTask = objectContent.ReadAsAsync();
            readObj = TaskAssert.SucceedsWithResult(readTask);
            Assert.AreEqual("mole data", readObj, "2nd ReadAsAsync did not return cached value.");
        }

        #endregion ReadAsAsync()

        #region ReadAsOrDefault()

        [TestMethod]
        [TestCategory("CIT"), Timeout(TimeoutConstant.DefaultTimeout), Owner("")]
        [Description("ReadAsOrDefault() gets the object instance provided to the constructor for all value and reference types.")]
        public void ReadAsOrDefaultGetsObject()
        {
            TestDataAssert.Execute(
                TestData.RepresentativeValueAndRefTypeTestDataCollection,
                (type, obj) =>
                {
                    ObjectContent content = new ObjectContent(type, obj);
                    object readObj = content.ReadAsOrDefault();
                    Assert.AreEqual(obj, readObj, string.Format("ReadAs failed for type '{0}'.", type.Name));
                });
        }

        [TestMethod]
        [TestCategory("CIT"), Timeout(TimeoutConstant.DefaultTimeout), Owner("")]
        [Description("ReadAsOrDefault() gets the default value for all value and reference types if no formatter is available.")]
        public void ReadAsOrDefaultGetsDefaultWithNoFormatter()
        {
            TestDataAssert.Execute(
                TestData.RepresentativeValueAndRefTypeTestDataCollection,
                (type, obj) =>
                {
                    if (!HttpTestData.IsKnownUnserializable(type, obj) && HttpTestData.CanRoundTrip(type))
                    {
                        StreamAssert.UsingXmlSerializer(
                            type,
                            obj,
                            (stream) =>
                            {
                                StreamContent streamContent = new StreamContent(stream);
                                streamContent.Headers.ContentType = new MediaTypeHeaderValue("application/unknownMediaType");
                                ObjectContent contentWrappingStream = new ObjectContent(type, streamContent);
                                object readObj = contentWrappingStream.ReadAsOrDefault();
                                object defaultObj = DefaultValue(type);
                                TestDataAssert.AreEqual(defaultObj, readObj, "Failed to get default value.");
                            });
                    }
                });
        }

        #endregion ReadAsOrDefault()

        #region ReadAsOrDefaultAsync()

        [TestMethod]
        [TestCategory("CIT"), Timeout(TimeoutConstant.DefaultTimeout), Owner("")]
        [Description("ReadAsOrDefaultAsync() gets a Task<object> returning object instance provided to the constructor for all value and reference types.")]
        public void ReadAsOrDefaultAsyncGetsTaskOfObject()
        {
            TestDataAssert.Execute(
                TestData.RepresentativeValueAndRefTypeTestDataCollection,
                (type, obj) =>
                {
                    ObjectContent content = new ObjectContent(type, obj);
                    Task<object> task = content.ReadAsOrDefaultAsync();
                    TaskAssert.ResultEquals(task, obj);
                });
        }

        [TestMethod]
        [TestCategory("CIT"), Timeout(TimeoutConstant.DefaultTimeout), Owner("")]
        [Description("ReadAsOrDefaultAsync() gets Task yielding default value for all value and reference types if no formatter is available.")]
        public void ReadAsOrDefaultAsyncGetsDefaultWithNoFormatter()
        {
            TestDataAssert.Execute(
                TestData.RepresentativeValueAndRefTypeTestDataCollection,
                (type, obj) =>
                {
                    if (!HttpTestData.IsKnownUnserializable(type, obj) && HttpTestData.CanRoundTrip(type))
                    {
                        StreamAssert.UsingXmlSerializer(
                            type,
                            obj,
                            (stream) =>
                            {
                                StreamContent streamContent = new StreamContent(stream);
                                streamContent.Headers.ContentType = new MediaTypeHeaderValue("application/unknownMediaType");
                                ObjectContent contentWrappingStream = new ObjectContent(type, streamContent);
                                Task<object> readTask = contentWrappingStream.ReadAsOrDefaultAsync();
                                object readObj = TaskAssert.SucceedsWithResult(readTask);
                                object defaultObj = DefaultValue(type);
                                TestDataAssert.AreEqual(defaultObj, readObj, "Failed to get default value.");
                            });
                    }
                });
        }

        #endregion ReadAsOrDefaultAsync()

        #endregion Methods

        #region Test helpers

        private static object DefaultValue(Type type)
        {
            Assert.IsNotNull(type, "type cannot be null.");
            if (!type.IsValueType)
            {
                return null;
            }

            return Activator.CreateInstance(type);
        }

        #endregion TestHelpers
    }
}
