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
    using System.Reflection;
    using System.Threading.Tasks;
    using Microsoft.ApplicationServer.Common.Test;
    using Microsoft.ApplicationServer.Http.Moles;
    using Microsoft.ApplicationServer.Http.Test;
    using Microsoft.VisualStudio.TestTools.UnitTesting;

    [TestClass, UnitTestLevel(UnitTestLevel.InProgress)]
    public class ObjectContentOfTTests : UnitTest<ObjectContent<object>>
    {
        private static readonly Type objectContentOfTType = typeof(ObjectContent<>);
        private static readonly string readAsMethodName = "ReadAs";
        private static readonly string readAsAsyncMethodName = "ReadAsAsync";
        private static readonly string readAsOrDefaultMethodName = "ReadAsOrDefault";
        private static readonly string readAsOrDefaultAsyncMethodName = "ReadAsOrDefaultAsync";

        [TestCleanup, HostType("Moles")]
        public void TestCleanup()
        {
            // Ensure every test resets Moled ObjectContent back to default
            MObjectContent.BehaveAsCurrent();
        }

        #region Type

        [TestMethod]
        [TestCategory("CIT"), Timeout(TimeoutConstant.DefaultTimeout), Owner("")]
        [Description("ObjectContent<T> is public, concrete, unsealed and generic.")]
        public void TypeIsCorrect()
        {
            Type t = this.TypeUnderTest;
            Assert.IsTrue(t.IsPublic, "ObjectContent<T> should be public.");
            Assert.IsFalse(t.IsAbstract, "ObjectContent<T> should not be abstract.");
            Assert.IsFalse(t.IsSealed, "ObjectContent should<T> not be sealed.");
            Assert.IsTrue(t.IsGenericType, "OjectContent<T> should be generic.");
            Assert.AreEqual(typeof(ObjectContent), typeof(ObjectContent<>).BaseType, "ObjectContent<T> base type should be ObjectContent.");
        }

        #endregion Type

        #region Constructors

        #region ObjectContent<T>(T)
        
        [TestMethod, HostType("Moles")]
        [TestCategory("CIT"), Timeout(TimeoutConstant.DefaultTimeout), Owner("")]
        [Description("ObjectContent<T>(T) sets Type and (private) ObjectInstance properties with all known value and reference types.  ContentType defaults to null.")]
        public void Constructor()
        {
            object ctorObject = null;
            MObjectContent.AllInstances.ValueSetObject = (@this, o) => ctorObject = o;

            TestDataAssert.Execute(
                TestData.RepresentativeValueAndRefTypeTestDataCollection,
                (type, obj) =>
                {
                    ObjectContent content = GenericTypeAssert.InvokeConstructor<ObjectContent>(objectContentOfTType, type, new Type[] { type }, new object[] { obj });
                    Assert.AreSame(type, content.Type, "Failed to set Type");
                    Assert.IsNull(content.Headers.ContentType, "ContentType should default to null.");
                    Assert.AreEqual(obj, ctorObject, "Failed to set ObjectInstance");
                });
        }

        [TestMethod, HostType("Moles")]
        [TestCategory("CIT"), Timeout(TimeoutConstant.DefaultTimeout), Owner("")]
        [Description("ObjectContent<T>(T) converts null value types to that type's default value and sets (private) ObjectInstance.")]
        public void ConstructorConvertsNullValueTypeToDefault()
        {
            object ctorObject = null;
            MObjectContent.AllInstances.ValueSetObject = (@this, o) => ctorObject = o;

            TestDataAssert.Execute(
                TestData.ValueTypeTestDataCollection,
                TestDataVariations.AsInstance,
                "Null value types should throw",
                (type, obj) =>
                {
                    ObjectContent content = GenericTypeAssert.InvokeConstructor<ObjectContent>(objectContentOfTType, type, new Type[] { type }, new object[] { null });
                    Assert.IsNotNull(ctorObject, "Setting null value type should have converted to default.");
                    Assert.AreEqual(type, ctorObject.GetType(), "Expected default object to be of generic parameter's type.");
                });
        }

        [TestMethod]
        [TestCategory("CIT"), Timeout(TimeoutConstant.DefaultTimeout), Owner("")]
        [Description("ObjectContent<T>(T) throws with HttpContent as T.")]
        public void ConstructorThrowsWithHttpContentAsT()
        {
            foreach (HttpContent httpContent in HttpTestData.StandardHttpContents)
            {
                ExceptionAssert.Throws<TargetInvocationException, ArgumentException>(
                    SR.CannotUseThisParameterType(typeof(HttpContent).Name, typeof(ObjectContent).Name),
                    () => GenericTypeAssert.InvokeConstructor(objectContentOfTType, httpContent.GetType(), new Type[] { httpContent.GetType() }, new object[] { httpContent }),
                    (ae) => Assert.AreEqual("type", ae.ParamName, "ParamName in exception was incorrect."));
            }
        }

        #endregion ObjectContent<T>(T)

        #region ObjectContent<T>(T, string)

        [TestMethod, HostType("Moles")]
        [TestCategory("CIT"), Timeout(TimeoutConstant.DefaultTimeout), Owner("")]
        [Description("ObjectContent<T>(T, string) sets Type, content header's media type and (private) ObjectInstance properties with all known value and reference types.")]
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
                        ObjectContent content = GenericTypeAssert.InvokeConstructor<ObjectContent>(
                                                    objectContentOfTType, 
                                                    type, 
                                                    new Type[] { type, typeof(string) }, 
                                                    new object[] { obj, mediaType });
                        Assert.AreSame(type, content.Type, "Failed to set Type.");
                        Assert.AreEqual(obj, ctorObject, "Failed to set ObjectInstance.");
                        MediaTypeAssert.AreEqual(content.Headers.ContentType, mediaType, "MediaType was not set.");
                    };
                });
        }

        [TestMethod, HostType("Moles")]
        [TestCategory("CIT"), Timeout(TimeoutConstant.DefaultTimeout), Owner("")]
        [Description("ObjectContent<T>(T, string) converts null value types to that type's default value and sets (private) ObjectInstance.")]
        public void Constructor1ConvertsNullValueTypeToDefault()
        {
            object ctorObject = null;
            MObjectContent.AllInstances.ValueSetObject = (@this, o) => ctorObject = o;

            TestDataAssert.Execute(
                TestData.ValueTypeTestDataCollection,
                TestDataVariations.AsInstance,
                "Null value types should throw",
                (type, obj) =>
                    {
                        foreach (string mediaType in HttpTestData.LegalMediaTypeStrings)
                        {
                            ObjectContent content = GenericTypeAssert.InvokeConstructor<ObjectContent>(
                                                        objectContentOfTType, 
                                                        type, 
                                                        new Type[] { type, typeof(string) }, 
                                                        new object[] { null, mediaType });
                            Assert.IsNotNull(ctorObject, "Setting null value type should have converted to default.");
                            Assert.AreEqual(type, ctorObject.GetType(), "Expected default object to be of generic parameter's type.");
                        }
                    });
        }

        [TestMethod]
        [TestCategory("CIT"), Timeout(TimeoutConstant.DefaultTimeout), Owner("")]
        [Description("ObjectContent<T>(T, string) throws with HttpContent as T.")]
        public void Constructor1ThrowsWithHttpContentAsT()
        {
            foreach (HttpContent httpContent in HttpTestData.StandardHttpContents)
            {
                foreach (string mediaType in HttpTestData.LegalMediaTypeStrings)
                {
                    ExceptionAssert.Throws<TargetInvocationException, ArgumentException>(
                        SR.CannotUseThisParameterType(typeof(HttpContent).Name, typeof(ObjectContent).Name),
                        () => GenericTypeAssert.InvokeConstructor(
                                objectContentOfTType, 
                                httpContent.GetType(), 
                                new Type[] { httpContent.GetType(), typeof(string) }, 
                                new object[] { httpContent, mediaType }),
                        (ae) => Assert.AreEqual("type", ae.ParamName, "ParamName in exception was incorrect."));
                }
            }
        }

        [TestMethod]
        [TestCategory("CIT"), Timeout(TimeoutConstant.DefaultTimeout), Owner("")]
        [Description("ObjectContent<T>(T, string) throws for an empty media type.")]
        public void Constructor1ThrowsWithEmptyMediaType()
        {
            TestDataAssert.Execute(
                TestData.RepresentativeValueAndRefTypeTestDataCollection,
                (type, obj) =>
                {
                    foreach (string mediaType in TestData.EmptyStrings)
                    {
                        ExceptionAssert.Throws<TargetInvocationException, ArgumentNullException>(
                            null,
                            () => GenericTypeAssert.InvokeConstructor(
                                    objectContentOfTType,
                                    type,
                                    new Type[] { type, typeof(string) },
                                    new object[] { obj, mediaType }),
                            (ae) => Assert.AreEqual("mediaType", ae.ParamName, "ParamName in exception was incorrect."));

                    };
            });
        }

        [TestMethod]
        [TestCategory("CIT"), Timeout(TimeoutConstant.DefaultTimeout), Owner("")]
        [Description("ObjectContent<T>(T, string) throws with an illegal media type.")]
        public void Constructor1ThrowsWithIllegalMediaType()
        {
            TestDataAssert.Execute(
                TestData.RepresentativeValueAndRefTypeTestDataCollection,
                (type, obj) =>
                {
                    foreach (string mediaType in HttpTestData.IllegalMediaTypeStrings)
                    {
                        ExceptionAssert.ThrowsArgument<TargetInvocationException>(
                            "mediaType",
                            SR.InvalidMediaType(mediaType, typeof(MediaTypeHeaderValue).Name),
                            () => GenericTypeAssert.InvokeConstructor(
                                    objectContentOfTType,
                                    type,
                                    new Type[] { type, typeof(string) },
                                    new object[] { obj, mediaType }));

                    };
            });
        }

        #endregion ObjectContent<T>(T, string)

        #region ObjectContent<T>(T, MediaTypeHeaderValue)

        [TestMethod, HostType("Moles")]
        [TestCategory("CIT"), Timeout(TimeoutConstant.DefaultTimeout), Owner("")]
        [Description("ObjectContent<T>(T, MediaTypeHeaderValue) sets Type, content header's media type and (private) ObjectInstance properties with all known value and reference types.")]
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
                        ObjectContent content = GenericTypeAssert.InvokeConstructor<ObjectContent>(
                                                    objectContentOfTType,
                                                    type,
                                                    new Type[] { type, typeof(MediaTypeHeaderValue) },
                                                    new object[] { obj, mediaType });
                        Assert.AreSame(type, content.Type, "Failed to set Type.");
                        Assert.AreEqual(obj, ctorObject, "Failed to set ObjectInstance.");
                        MediaTypeAssert.AreEqual(content.Headers.ContentType, mediaType, "MediaType was not set.");
                    };
                });
        }


        [TestMethod, HostType("Moles")]
        [TestCategory("CIT"), Timeout(TimeoutConstant.DefaultTimeout), Owner("")]
        [Description("ObjectContent<T>(T, MediaTypeHeaderValue) converts null value types to that type's default value and sets (private) ObjectInstance.")]
        public void Constructor2ConvertsNullValueTypeToDefault()
        {
            object ctorObject = null;
            MObjectContent.AllInstances.ValueSetObject = (@this, o) => ctorObject = o;

            TestDataAssert.Execute(
                TestData.ValueTypeTestDataCollection,
                TestDataVariations.AsInstance,
                "Null value types should throw",
                (type, obj) =>
                {
                    foreach (MediaTypeHeaderValue mediaType in HttpTestData.LegalMediaTypeHeaderValues)
                    {
                        ObjectContent content = GenericTypeAssert.InvokeConstructor<ObjectContent>(
                                                    objectContentOfTType,
                                                    type,
                                                    new Type[] { type, typeof(MediaTypeHeaderValue) },
                                                    new object[] { null, mediaType });
                        Assert.IsNotNull(ctorObject, "Setting null value type should have converted to default.");
                        Assert.AreEqual(type, ctorObject.GetType(), "Expected default object to be of generic parameter's type.");
                    }
                });
        }

        [TestMethod]
        [TestCategory("CIT"), Timeout(TimeoutConstant.DefaultTimeout), Owner("")]
        [Description("ObjectContent<T>(T, MediaTypeHeaderValue) throws with HttpContent as T.")]
        public void Constructor2ThrowsWithHttpContentAsT()
        {
            foreach (HttpContent httpContent in HttpTestData.StandardHttpContents)
            {
                foreach (MediaTypeHeaderValue mediaType in HttpTestData.LegalMediaTypeHeaderValues)
                {
                    ExceptionAssert.Throws<TargetInvocationException, ArgumentException>(
                        SR.CannotUseThisParameterType(typeof(HttpContent).Name, typeof(ObjectContent).Name),
                        () => GenericTypeAssert.InvokeConstructor(
                                objectContentOfTType,
                                httpContent.GetType(),
                                new Type[] { httpContent.GetType(), typeof(MediaTypeHeaderValue) },
                                new object[] { httpContent, mediaType }),
                        (ae) => Assert.AreEqual("type", ae.ParamName, "ParamName in exception was incorrect."));
                }
            }
        }

        [TestMethod]
        [TestCategory("CIT"), Timeout(TimeoutConstant.DefaultTimeout), Owner("")]
        [Description("ObjectContent<T>(T, MediaTypeHeaderValue) throws for a null media type.")]
        public void Constructor2ThrowsWithNullMediaType()
        {
            ExceptionAssert.ThrowsArgumentNull(
                "mediaType",
                () => new ObjectContent<int>(5, (MediaTypeHeaderValue)null));
        }

        #endregion ObjectContent<T>(T, MediaTypeHeaderValue)

        #region ObjectContent<T>(HttpContent)

        [TestMethod, HostType("Moles")]
        [TestCategory("CIT"), Timeout(TimeoutConstant.DefaultTimeout), Owner("")]
        [Description("ObjectContent<T>(HttpContent) sets Type, MediaType property for all known value and reference types and all standard HttpContent types.")]
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
                        ObjectContent content = GenericTypeAssert.InvokeConstructor<ObjectContent>(
                                                    objectContentOfTType, 
                                                    type, 
                                                    new Type[] { typeof(HttpContent) },
                                                    new object[] { httpContent });
                        Assert.AreSame(type, content.Type, "Failed to set Type");
                        Assert.AreEqual(httpContent, ctorHttpContent, "Failed to set HttpContent");
                        MediaTypeAssert.AreEqual(content.Headers.ContentType, httpContent.Headers.ContentType, "MediaType was not set.");
                    }
                });
        }

        [TestMethod]
        [TestCategory("CIT"), Timeout(TimeoutConstant.DefaultTimeout), Owner("")]
        [Description("ObjectContent<T>(HttpContent) sets ContentHeaders with input HttpContent.")]
        public void Constructor3SetsContentHeadersWithHttpContent()
        {
            TestDataAssert.Execute(
                TestData.RepresentativeValueAndRefTypeTestDataCollection,
                (type, obj) =>
                {
                    foreach (HttpContent httpContent in HttpTestData.StandardHttpContents)
                    {
                        httpContent.Headers.Add("CIT-Name", "CIT-Value");
                        ObjectContent content = GenericTypeAssert.InvokeConstructor<ObjectContent>(
                                                    objectContentOfTType,
                                                    type,
                                                    new Type[] { typeof(HttpContent) },
                                                    new object[] { httpContent });
                        HttpAssert.Contains(content.Headers, "CIT-Name", "CIT-Value");
                    }
                });
        }

        [TestMethod]
        [TestCategory("CIT"), Timeout(TimeoutConstant.DefaultTimeout), Owner("")]
        [Description("ObjectContent<T>(HttpContent) throws with HttpContent as T.")]
        public void Constructor3ThrowsWithHttpContentAsT()
        {
            foreach (HttpContent httpContent in HttpTestData.StandardHttpContents)
            {
                ExceptionAssert.Throws<TargetInvocationException, ArgumentException>(
                    SR.CannotUseThisParameterType(typeof(HttpContent).Name, typeof(ObjectContent).Name),
                    () => GenericTypeAssert.InvokeConstructor(
                            objectContentOfTType, 
                            httpContent.GetType(), 
                            new Type[] { typeof(HttpContent) }, 
                            new object[] { httpContent }),
                    (ae) => Assert.AreEqual("type", ae.ParamName, "ParamName in exception was incorrect."));
            }
        }

        [TestMethod]
        [TestCategory("CIT"), Timeout(TimeoutConstant.DefaultTimeout), Owner("")]
        [Description("ObjectContent<T>(HttpContent) throws with a null HttpContent.")]
        public void Constructor3ThrowsWithNullHttpContent()
        {
            ExceptionAssert.ThrowsArgumentNull(
                "content",
                () => new ObjectContent<int>((HttpContent)null));
        }

        #endregion ObjectContent<T>(HttpContent)

        #region ObjectContent<T>(T, IEnumerable<MediaTypeFormatter>)

        [TestMethod, HostType("Moles")]
        [TestCategory("CIT"), Timeout(TimeoutConstant.DefaultTimeout), Owner("")]
        [Description("ObjectContent<T>(T, IEnumerable<MediaTypeFormatter>) sets Type, Formatters and (private)  ObjectInstance properties with all known value and reference types.  ContentType defaults to null.")]
        public void Constructor4()
        {
            object ctorObject = null;
            MObjectContent.AllInstances.ValueSetObject = (@this, o) => ctorObject = o;

            TestDataAssert.Execute(
                TestData.RepresentativeValueAndRefTypeTestDataCollection,
                (type, obj) =>
                {
                    foreach (IEnumerable<MediaTypeFormatter> formatterCollection in HttpTestData.AllFormatterCollections)
                    {
                        // eval to force stable instances
                        MediaTypeFormatter[] formatters = formatterCollection.ToArray();

                        ObjectContent content = GenericTypeAssert.InvokeConstructor<ObjectContent>(
                                                    objectContentOfTType, 
                                                    type, 
                                                    new Type[] { type, typeof(IEnumerable<MediaTypeFormatter>) }, 
                                                    new object[] { obj, formatters });
                        Assert.AreSame(type, content.Type, "Failed to set Type.");
                        Assert.AreEqual(obj, ctorObject, "Failed to set ObjectInstance.");
                        Assert.IsNotNull(content.Formatters, "Failed to set Formatters.");
                        CollectionAssert.IsSubsetOf(formatters.ToList(), content.Formatters, "Formatters did not include all input formatters.");
                        Assert.IsNull(content.Headers.ContentType, "ContentType should default to null.");
                    }
                });
        }

        [TestMethod, HostType("Moles")]
        [TestCategory("CIT"), Timeout(TimeoutConstant.DefaultTimeout), Owner("")]
        [Description("ObjectContent<T>(T, IEnumerable<MediaTypeFormatter>) converts null value types to that type's default value and sets (private) ObjectInstance.")]
        public void Constructor4ConvertsNullValueTypeToDefault()
        {
            object ctorObject = null;
            MObjectContent.AllInstances.ValueSetObject = (@this, o) => ctorObject = o;

            TestDataAssert.Execute(
                TestData.ValueTypeTestDataCollection,
                TestDataVariations.AsInstance,
                "Null value types should throw",
                (type, obj) =>
                {
                    foreach (IEnumerable<MediaTypeFormatter> formatterCollection in HttpTestData.AllFormatterCollections)
                    {
                        // eval to force stable instances
                        MediaTypeFormatter[] formatters = formatterCollection.ToArray();
                        ObjectContent content = GenericTypeAssert.InvokeConstructor<ObjectContent>(
                                                    objectContentOfTType, 
                                                    type, 
                                                    new Type[] { type, typeof(IEnumerable<MediaTypeFormatter>) }, 
                                                    new object[] { null, formatters });
                        Assert.IsNotNull(ctorObject, "Setting null value type should have converted to default.");
                        Assert.AreEqual(type, ctorObject.GetType(), "Expected default object to be of generic parameter's type.");
                        Assert.IsNotNull(content.Formatters, "Failed to set Formatters.");
                        CollectionAssert.IsSubsetOf(formatters.ToList(), content.Formatters, "Formatters did not include all input formatters.");
                    }
                });
        }

        [TestMethod, HostType("Moles")]
        [TestCategory("CIT"), Timeout(TimeoutConstant.DefaultTimeout), Owner("")]
        [Description("ObjectContent<T>(T, IEnumerable<MediaTypeFormatter>) throws if formatters parameter is null.")]
        public void Constructor4ThrowsWithNullFormatters()
        {
            ExceptionAssert.ThrowsArgumentNull(
                "formatters",
                () => new ObjectContent<int>(5, (IEnumerable<MediaTypeFormatter>)null));
        }

        [TestMethod]
        [TestCategory("CIT"), Timeout(TimeoutConstant.DefaultTimeout), Owner("")]
        [Description("ObjectContent<T>(T, IEnumerable<MediaTypeFormatter>) throws with HttpContent as T.")]
        public void Constructor4ThrowsWithHttpContentAsT()
        {
            foreach (HttpContent httpContent in HttpTestData.StandardHttpContents)
            {
                ExceptionAssert.Throws<TargetInvocationException, ArgumentException>(
                    SR.CannotUseThisParameterType(typeof(HttpContent).Name, typeof(ObjectContent).Name),
                    () => GenericTypeAssert.InvokeConstructor(
                            objectContentOfTType, 
                            httpContent.GetType(), 
                            new Type[] { httpContent.GetType() }, 
                            new object[] { httpContent }),
                    (ae) => Assert.AreEqual("type", ae.ParamName, "ParamName in exception was incorrect"));
            }
        }

        #endregion ObjectContent<T>(T, IEnumerable<MediaTypeFormatter>)

        #region ObjectContent<T>(T, string, IEnumerable<MediaTypeFormatter>)

        [TestMethod, HostType("Moles")]
        [TestCategory("CIT"), Timeout(TimeoutConstant.DefaultTimeout), Owner("")]
        [Description("ObjectContent<T>(T, string, IEnumerable<MediaTypeFormatter>) sets Type, content header's media type, Formatters and (private) ObjectInstance properties with all known value and reference types.")]
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
                        foreach (IEnumerable<MediaTypeFormatter> formatterCollections in HttpTestData.AllFormatterCollections)
                        {
                            MediaTypeFormatter[] formatters = formatterCollections.ToArray();

                            ObjectContent content = GenericTypeAssert.InvokeConstructor<ObjectContent>(
                                                        objectContentOfTType,
                                                        type,
                                                        new Type[] { type, typeof(string), typeof(IEnumerable<MediaTypeFormatter>) },
                                                        new object[] { obj, mediaType, formatters });
                            Assert.AreSame(type, content.Type, "Failed to set Type.");
                            Assert.AreEqual(obj, ctorObject, "Failed to set ObjectInstance.");
                            MediaTypeAssert.AreEqual(content.Headers.ContentType, mediaType, "MediaType was not set.");
                            CollectionAssert.IsSubsetOf(formatters, content.Formatters, "Failed to use all formatters specified.");
                        }
                    };
                });
        }

        [TestMethod, HostType("Moles")]
        [TestCategory("CIT"), Timeout(TimeoutConstant.DefaultTimeout), Owner("")]
        [Description("ObjectContent<T>(T, string, IEnumerable<MediaTypeFormatter>) converts null value types to that type's default value and sets (private) ObjectInstance.")]
        public void Constructor5ConvertsNullValueTypeToDefault()
        {
            object ctorObject = null;
            MObjectContent.AllInstances.ValueSetObject = (@this, o) => ctorObject = o;

            TestDataAssert.Execute(
                TestData.ValueTypeTestDataCollection,
                TestDataVariations.AsInstance,
                "Null value types should throw",
                (type, obj) =>
                {
                    foreach (string mediaType in HttpTestData.LegalMediaTypeStrings)
                    {
                        foreach (IEnumerable<MediaTypeFormatter> formatterCollections in HttpTestData.AllFormatterCollections)
                        {
                            MediaTypeFormatter[] formatters = formatterCollections.ToArray();

                            ObjectContent content = GenericTypeAssert.InvokeConstructor<ObjectContent>(
                                                        objectContentOfTType,
                                                        type,
                                                        new Type[] { type, typeof(string), typeof(IEnumerable<MediaTypeFormatter>) },
                                                        new object[] { null, mediaType, formatters });
                            Assert.IsNotNull(ctorObject, "Setting null value type should have converted to default.");
                            Assert.AreEqual(type, ctorObject.GetType(), "Expected default object to be of generic parameter's type.");
                        }
                    }
                });
        }

        [TestMethod]
        [TestCategory("CIT"), Timeout(TimeoutConstant.DefaultTimeout), Owner("")]
        [Description("ObjectContent<T>(T, string, IEnumerable<MediaTypeFormatter>) throws with HttpContent as T.")]
        public void Constructor5ThrowsWithHttpContentAsT()
        {
            foreach (HttpContent httpContent in HttpTestData.StandardHttpContents)
            {
                foreach (string mediaType in HttpTestData.LegalMediaTypeStrings)
                {
                    foreach (IEnumerable<MediaTypeFormatter> formatters in HttpTestData.AllFormatterCollections)
                    {
                        ExceptionAssert.Throws<TargetInvocationException, ArgumentException>(
                            SR.CannotUseThisParameterType(typeof(HttpContent).Name, typeof(ObjectContent).Name),
                            () => GenericTypeAssert.InvokeConstructor(
                                    objectContentOfTType,
                                    httpContent.GetType(),
                                    new Type[] { httpContent.GetType(), typeof(string), typeof(IEnumerable<MediaTypeFormatter>) },
                                    new object[] { httpContent, mediaType, formatters }),
                            (ae) => Assert.AreEqual("type", ae.ParamName, "ParamName in exception was incorrect."));
                    }
                }
            }
        }

        [TestMethod]
        [TestCategory("CIT"), Timeout(TimeoutConstant.DefaultTimeout), Owner("")]
        [Description("ObjectContent<T>(T, string, IEnumerable<MediaTypeFormatter>) throws for an empty media type.")]
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
                            ExceptionAssert.Throws<TargetInvocationException, ArgumentNullException>(
                                null,
                                () => GenericTypeAssert.InvokeConstructor(
                                        objectContentOfTType,
                                        type,
                                        new Type[] { type, typeof(string), typeof(IEnumerable<MediaTypeFormatter>) },
                                        new object[] { obj, mediaType, formatters }),
                                (ae) => Assert.AreEqual("mediaType", ae.ParamName, "ParamName in exception was incorrect."));
                        }
                    };
                });
        }

        [TestMethod]
        [TestCategory("CIT"), Timeout(TimeoutConstant.DefaultTimeout), Owner("")]
        [Description("ObjectContent<T>(T, string, IEnumerable<MediaTypeFormatter>) throws with an illegal media type.")]
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
                            ExceptionAssert.ThrowsArgument<TargetInvocationException>(
                                "mediaType",
                                SR.InvalidMediaType(mediaType, typeof(MediaTypeHeaderValue).Name),
                                () => GenericTypeAssert.InvokeConstructor(
                                        objectContentOfTType,
                                        type,
                                        new Type[] { type, typeof(string), typeof(IEnumerable<MediaTypeFormatter>) },
                                        new object[] { obj, mediaType, formatters }));
                        }
                    };
                });
        }

        [TestMethod, HostType("Moles")]
        [TestCategory("CIT"), Timeout(TimeoutConstant.DefaultTimeout), Owner("")]
        [Description("ObjectContent<T>(T, string, IEnumerable<MediaTypeFormatter>) throws if formatters parameter is null.")]
        public void Constructor5ThrowsWithNullFormatters()
        {
            foreach (string mediaType in HttpTestData.LegalMediaTypeStrings)
            {
                ExceptionAssert.ThrowsArgumentNull(
                    "formatters",
                    () => new ObjectContent<int>(5, mediaType, (IEnumerable<MediaTypeFormatter>)null));
            }
        }

        #endregion ObjectContent<T>(T, string, IEnumerable<MediaTypeFormatter>)

        #region ObjectContent<T>(T, MediaTypeHeaderValue, IEnumerable<MediaTypeFormatter>)

        [TestMethod, HostType("Moles")]
        [TestCategory("CIT"), Timeout(TimeoutConstant.DefaultTimeout), Owner("")]
        [Description("ObjectContent<T>(T, MediaTypeHeaderValue, IEnumerable<MediaTypeFormatter>) sets Type, content header's media type, Formatters and (private) ObjectInstance properties with all known value and reference types.")]
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
                        foreach (IEnumerable<MediaTypeFormatter> formatterCollections in HttpTestData.AllFormatterCollections)
                        {
                            MediaTypeFormatter[] formatters = formatterCollections.ToArray();

                            ObjectContent content = GenericTypeAssert.InvokeConstructor<ObjectContent>(
                                                        objectContentOfTType,
                                                        type,
                                                        new Type[] { type, typeof(MediaTypeHeaderValue), typeof(IEnumerable<MediaTypeFormatter>) },
                                                        new object[] { obj, mediaType, formatters });
                            Assert.AreSame(type, content.Type, "Failed to set Type.");
                            Assert.AreEqual(obj, ctorObject, "Failed to set ObjectInstance.");
                            MediaTypeAssert.AreEqual(content.Headers.ContentType, mediaType, "MediaType was not set.");
                            CollectionAssert.IsSubsetOf(formatters, content.Formatters, "Failed to use all formatters specified.");
                        }
                    };
                });
        }

        [TestMethod, HostType("Moles")]
        [TestCategory("CIT"), Timeout(TimeoutConstant.DefaultTimeout), Owner("")]
        [Description("ObjectContent<T>(T, MediaTypeHeaderValue, IEnumerable<MediaTypeFormatter>) converts null value types to that type's default value and sets (private) ObjectInstance.")]
        public void Constructor6ConvertsNullValueTypeToDefault()
        {
            object ctorObject = null;
            MObjectContent.AllInstances.ValueSetObject = (@this, o) => ctorObject = o;

            TestDataAssert.Execute(
                TestData.ValueTypeTestDataCollection,
                TestDataVariations.AsInstance,
                "Null value types should throw",
                (type, obj) =>
                {
                    foreach (MediaTypeHeaderValue mediaType in HttpTestData.LegalMediaTypeHeaderValues)
                    {
                        foreach (IEnumerable<MediaTypeFormatter> formatterCollections in HttpTestData.AllFormatterCollections)
                        {
                            MediaTypeFormatter[] formatters = formatterCollections.ToArray();

                            ObjectContent content = GenericTypeAssert.InvokeConstructor<ObjectContent>(
                                                        objectContentOfTType,
                                                        type,
                                                        new Type[] { type, typeof(MediaTypeHeaderValue), typeof(IEnumerable<MediaTypeFormatter>) },
                                                        new object[] { null, mediaType, formatters });
                            Assert.IsNotNull(ctorObject, "Setting null value type should have converted to default.");
                            Assert.AreEqual(type, ctorObject.GetType(), "Expected default object to be of generic parameter's type.");
                        }
                    }
                });
        }

        [TestMethod]
        [TestCategory("CIT"), Timeout(TimeoutConstant.DefaultTimeout), Owner("")]
        [Description("ObjectContent<T>(T, MediaTypeHeaderValue, IEnumerable<MediaTypeFormatter>) throws with HttpContent as T.")]
        public void Constructor6ThrowsWithHttpContentAsT()
        {
            foreach (HttpContent httpContent in HttpTestData.StandardHttpContents)
            {
                foreach (MediaTypeHeaderValue mediaType in HttpTestData.LegalMediaTypeHeaderValues)
                {
                    foreach (IEnumerable<MediaTypeFormatter> formatters in HttpTestData.AllFormatterCollections)
                    {
                        ExceptionAssert.Throws<TargetInvocationException, ArgumentException>(
                            SR.CannotUseThisParameterType(typeof(HttpContent).Name, typeof(ObjectContent).Name),
                            () => GenericTypeAssert.InvokeConstructor(
                                    objectContentOfTType,
                                    httpContent.GetType(),
                                    new Type[] { httpContent.GetType(), typeof(MediaTypeHeaderValue), typeof(IEnumerable<MediaTypeFormatter>) },
                                    new object[] { httpContent, mediaType, formatters }),
                            (ae) => Assert.AreEqual("type", ae.ParamName, "ParamName in exception was incorrect."));
                    }
                }
            }
        }

        [TestMethod]
        [TestCategory("CIT"), Timeout(TimeoutConstant.DefaultTimeout), Owner("")]
        [Description("ObjectContent<T>(T, MediaTypeHeaderValue, IEnumerable<MediaTypeFormatter>) throws for a null media type.")]
        public void Constructor6ThrowsWithNullMediaType()
        {
            foreach (IEnumerable<MediaTypeFormatter> formatters in HttpTestData.AllFormatterCollections)
            {
                ExceptionAssert.ThrowsArgumentNull(
                    "mediaType",
                    () => new ObjectContent<int>(5, (MediaTypeHeaderValue)null, formatters));
            }
        }

        [TestMethod, HostType("Moles")]
        [TestCategory("CIT"), Timeout(TimeoutConstant.DefaultTimeout), Owner("")]
        [Description("ObjectContent<T>(T, MediaTypeHeaderValue, IEnumerable<MediaTypeFormatter>) throws if formatters parameter is null.")]
        public void Constructor6ThrowsWithNullFormatters()
        {
            foreach (MediaTypeHeaderValue mediaType in HttpTestData.LegalMediaTypeHeaderValues)
            {
                ExceptionAssert.ThrowsArgumentNull(
                    "formatters",
                    () => new ObjectContent<int>(5, mediaType, (IEnumerable<MediaTypeFormatter>)null));
            }
        }

        #endregion ObjectContent<T>(T, MediaTypeHeaderValue, IEnumerable<MediaTypeFormatter>)

        #region ObjectContent<T>(HttpContent,IEnumerable<MediaTypeFormatter>)

        [TestMethod, HostType("Moles")]
        [TestCategory("CIT"), Timeout(TimeoutConstant.DefaultTimeout), Owner("")]
        [Description("ObjectContent<T>(HttpContent, IEnumerable<MediaTypeFormatter>) sets HttpContent and Formatter properties for all known value and reference types and all standard HttpContent types.")]
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
                        foreach (IEnumerable<MediaTypeFormatter> formatterCollections in HttpTestData.AllFormatterCollections)
                        {
                            MediaTypeFormatter[] formatters = formatterCollections.ToArray();
                            ObjectContent content = GenericTypeAssert.InvokeConstructor<ObjectContent>(
                                                        objectContentOfTType,
                                                        type,
                                                        new Type[] { typeof(HttpContent), typeof(IEnumerable<MediaTypeFormatter>) },
                                                        new object[] { httpContent, formatters });
                            Assert.AreSame(type, content.Type, "Failed to set Type.");
                            Assert.AreEqual(httpContent, ctorHttpContent, "Failed to set HttpContent.");
                            CollectionAssert.IsSubsetOf(formatters, content.Formatters, "Failed to use all input formatters.");
                            MediaTypeAssert.AreEqual(content.Headers.ContentType, httpContent.Headers.ContentType, "MediaType was not set.");
                        }
                    }
                });
        }

        [TestMethod]
        [TestCategory("CIT"), Timeout(TimeoutConstant.DefaultTimeout), Owner("")]
        [Description("ObjectContent<T>(HttpContent, IEnumerable<MediaTypeFormatter>) sets ContentHeaders with input HttpContent.")]
        public void Constructor7SetsContentHeadersWithHttpContent()
        {
            TestDataAssert.Execute(
                TestData.RepresentativeValueAndRefTypeTestDataCollection,
                (type, obj) =>
                {
                    foreach (HttpContent httpContent in HttpTestData.StandardHttpContents)
                    {
                        httpContent.Headers.Add("CIT-Name", "CIT-Value");
                        foreach (IEnumerable<MediaTypeFormatter> formatterCollections in HttpTestData.AllFormatterCollections)
                        {
                            MediaTypeFormatter[] formatters = formatterCollections.ToArray();
                            ObjectContent content = GenericTypeAssert.InvokeConstructor<ObjectContent>(
                                                        objectContentOfTType,
                                                        type,
                                                        new Type[] { typeof(HttpContent), typeof(IEnumerable<MediaTypeFormatter>) },
                                                        new object[] { httpContent, formatters });
                            HttpAssert.Contains(content.Headers, "CIT-Name", "CIT-Value");
                        }
                    }
                });
        }

        [TestMethod]
        [TestCategory("CIT"), Timeout(TimeoutConstant.DefaultTimeout), Owner("")]
        [Description("ObjectContent<T>(HttpContent, IEnumerable<MediaTypeFormatter>) throws with HttpContent as T.")]
        public void Constructor7ThrowsWithHttpContentAsT()
        {
            foreach (HttpContent httpContent in HttpTestData.StandardHttpContents)
            {
                foreach (IEnumerable<MediaTypeFormatter> formatters in HttpTestData.AllFormatterCollections)
                {
                    ExceptionAssert.Throws<TargetInvocationException, ArgumentException>(
                        SR.CannotUseThisParameterType(typeof(HttpContent).Name, typeof(ObjectContent).Name),
                        () => GenericTypeAssert.InvokeConstructor(
                                objectContentOfTType,
                                httpContent.GetType(),
                                new Type[] { typeof(HttpContent), typeof(IEnumerable<MediaTypeFormatter>) },
                                new object[] { httpContent, formatters }),
                        (ae) => Assert.AreEqual("type", ae.ParamName, "ParamName in exception was incorrect."));
                }
            }
        }

        [TestMethod]
        [TestCategory("CIT"), Timeout(TimeoutConstant.DefaultTimeout), Owner("")]
        [Description("ObjectContent<T>(HttpContent, IEnumerable<MediaTypeFormatter>) throws with a null HttpContent.")]
        public void Constructor7ThrowsWithNullHttpContent()
        {
            foreach (IEnumerable<MediaTypeFormatter> formatters in HttpTestData.AllFormatterCollections)
            {
                ExceptionAssert.ThrowsArgumentNull(
                    "content",
                    () => new ObjectContent<int>((HttpContent)null, formatters));
            }
        }

        [TestMethod]
        [TestCategory("CIT"), Timeout(TimeoutConstant.DefaultTimeout), Owner("")]
        [Description("ObjectContent<T>(HttpContent, IEnumerable<MediaTypeFormatter>) throws with a null Formatters.")]
        public void Constructor7ThrowsWithNullFormatters()
        {
            foreach (HttpContent httpContent in HttpTestData.StandardHttpContents)
            {
                ExceptionAssert.ThrowsArgumentNull(
                    "formatters",
                    () => new ObjectContent<int>(httpContent, (IEnumerable<MediaTypeFormatter>)null));
            }
        }

        #endregion ObjectContent<T>(HttpContent,IEnumerable<MediaTypeFormatter>)

        #endregion Constructors

        #region Methods

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
                    ObjectContent content = GenericTypeAssert.InvokeConstructor<ObjectContent>(objectContentOfTType, type, new Type[] { type }, new object[] { obj });
                    object readObj = GenericTypeAssert.InvokeMethod(content, readAsMethodName);
                    Assert.AreEqual(obj, readObj, string.Format("ReadAs failed for type '{0}'.", type.Name));
                });
        }

        [TestMethod, HostType("Moles")]
        [TestCategory("CIT"), Timeout(TimeoutConstant.DefaultTimeout), Owner("")]
        [Description("ReadAs() calls the registered MediaTypeFormatter for SupportedMediaTypes and ReadFromStream.")]
        public void ReadAsCallsFormatter()
        {
            SStringContent httpContent = new SStringContent("data");
            Collection<MediaTypeHeaderValue> mediaTypeCollection = new Collection<MediaTypeHeaderValue>() { httpContent.Headers.ContentType };

            bool askedForSupportedMediaTypes = false;
            SMediaTypeFormatter stubFormatter = new SMediaTypeFormatter();
            stubFormatter.OnCanReadTypeType = (type) => true;

            MMediaTypeFormatter formatter = new MMediaTypeFormatter(stubFormatter);
            formatter.SupportedMediaTypesGet = () => { askedForSupportedMediaTypes = true; return mediaTypeCollection; };
            formatter.ReadFromStreamTypeStreamHttpContentHeaders = (type, stream, headers) => "mole data";

            ObjectContent<string> objectContent = new ObjectContent<string>(httpContent);
            MediaTypeFormatterCollection formatterCollection = objectContent.Formatters;
            formatterCollection.Clear();
            formatterCollection.Add(formatter);

            // This statement should call CanReadType, get SupportedMediaTypes, discover the formatter and call it
            string readObj = objectContent.ReadAs();

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
            HttpContent httpContent = (HttpContent)moleContent;

            Collection<MediaTypeHeaderValue> mediaTypeCollection = new Collection<MediaTypeHeaderValue>() { httpContent.Headers.ContentType };

            bool askedForSupportedMediaTypes = false;
            SMediaTypeFormatter stubFormatter = new SMediaTypeFormatter();
            stubFormatter.OnCanReadTypeType = (type) => true;

            MMediaTypeFormatter formatter = new MMediaTypeFormatter(stubFormatter);
            formatter.SupportedMediaTypesGet = () => { askedForSupportedMediaTypes = true; return mediaTypeCollection; };
            formatter.ReadFromStreamTypeStreamHttpContentHeaders = (type, stream, headers) => "mole data";

            ObjectContent<string> objectContent = new ObjectContent<string>(httpContent);
            MediaTypeFormatterCollection formatterCollection = objectContent.Formatters;
            formatterCollection.Clear();
            formatterCollection.Add(formatter);

            // This statement should call CanReadType, get SupportedMediaTypes, discover the formatter and call it
            string readObj = objectContent.ReadAs();

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

                               // ObjectContent contentWrappingStream = new ObjectContent<T>(streamContent);
                               ObjectContent wrappingContent = GenericTypeAssert.InvokeConstructor<ObjectContent>(
                                                                   objectContentOfTType,
                                                                   type,
                                                                   streamContent);

                               string errorMessage = SR.NoReadSerializerAvailable(typeof(MediaTypeFormatter).Name, type.Name, "application/unknownMediaType");
                               ExceptionAssert.Throws<TargetInvocationException, InvalidOperationException>(
                                   errorMessage,
                                   // wrappingContent.ReadAsObj()
                                   () => GenericTypeAssert.InvokeMethod(wrappingContent, readAsMethodName));
                           });
                    }
                });
        }

        #endregion ReadAs()

        #region ReadAsAsync

        [TestMethod]
        [TestCategory("CIT"), Timeout(TimeoutConstant.DefaultTimeout), Owner("")]
        [Description("ReadAsAsync() gets a Task<object> returning object instance provided to the constructor for all value and reference types.")]
        public void ReadAsAsyncGetsTaskOfObject()
        {
            TestDataAssert.Execute(
                TestData.RepresentativeValueAndRefTypeTestDataCollection,
                (type, obj) =>
                {
                    ObjectContent content = GenericTypeAssert.InvokeConstructor<ObjectContent>(objectContentOfTType, type, new Type[] { type }, new object[] { obj });
                    Task task = GenericTypeAssert.InvokeMethod(content, readAsAsyncMethodName) as Task;
                    TaskAssert.ResultEquals(task, obj);
                });
        }

        [TestMethod, HostType("Moles")]
        [TestCategory("CIT"), Timeout(TimeoutConstant.DefaultTimeout), Owner("")]
        [Description("ReadAsAsync() calls the registered MediaTypeFormatter for SupportedMediaTypes and ReadFromStream.")]
        public void ReadAsAsyncCallsFormatter()
        {
            SStringContent httpContent = new SStringContent("data");
            Collection<MediaTypeHeaderValue> mediaTypeCollection = new Collection<MediaTypeHeaderValue>() { httpContent.Headers.ContentType };

            bool askedForSupportedMediaTypes = false;
            SMediaTypeFormatter stubFormatter = new SMediaTypeFormatter();
            stubFormatter.OnCanReadTypeType = (type) => true;
            stubFormatter.OnReadFromStreamAsyncTypeStreamHttpContentHeaders = (type, stream, headers) => Task.Factory.StartNew<object>(() => "mole data");

            MMediaTypeFormatter formatter = new MMediaTypeFormatter(stubFormatter);
            formatter.SupportedMediaTypesGet = () => { askedForSupportedMediaTypes = true; return mediaTypeCollection; };

            ObjectContent<string> objectContent = new ObjectContent<string>(httpContent);
            MediaTypeFormatterCollection formatterCollection = objectContent.Formatters;
            formatterCollection.Clear();
            formatterCollection.Add(formatter);

            // This statement should call CanReadType, get SupportedMediaTypes, discover the formatter and call it
            Task<string> readTask = objectContent.ReadAsAsync();
            string readObj = TaskAssert.SucceedsWithResult<string>(readTask);

            Assert.IsTrue(askedForSupportedMediaTypes, "ReadAs did not ask for supported media types.");
            Assert.AreEqual("mole data", readObj, "ReadAs did not return what the formatter returned.");
        }

        [TestMethod, HostType("Moles")]
        [TestCategory("CIT"), Timeout(TimeoutConstant.DefaultTimeout), Owner("")]
        [Description("ReadAsAsync() calls the registered MediaTypeFormatter for SupportedMediaTypes and ReadFromStream only once, and then uses the cached value.")]
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
            stubFormatter.OnReadFromStreamAsyncTypeStreamHttpContentHeaders = (type, stream, headers) => Task.Factory.StartNew<object>(() => "mole data");

            MMediaTypeFormatter formatter = new MMediaTypeFormatter(stubFormatter);
            formatter.SupportedMediaTypesGet = () => { askedForSupportedMediaTypes = true; return mediaTypeCollection; };

            ObjectContent<string> objectContent = new ObjectContent<string>(httpContent);
            MediaTypeFormatterCollection formatterCollection = objectContent.Formatters;
            formatterCollection.Clear();
            formatterCollection.Add(formatter);

            // This statement should call CanReadType, get SupportedMediaTypes, discover the formatter and call it
            Task<string> readTask = objectContent.ReadAsAsync();
            string readObj = TaskAssert.SucceedsWithResult<string>(readTask);

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

            stubFormatter.OnReadFromStreamAsyncTypeStreamHttpContentHeaders = (type, stream, headers) =>
            {
                Assert.Fail("2nd read should not call formatter.ReadFromStreamAsync.");
                return null;
            };

            readTask = objectContent.ReadAsAsync();
            readObj = TaskAssert.SucceedsWithResult<string>(readTask);
            Assert.AreEqual("mole data", readObj, "ReadAs did not return the cached value.");
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

                               // ObjectContent contentWrappingStream = new ObjectContent<T>(streamContent);
                               ObjectContent wrappingContent = GenericTypeAssert.InvokeConstructor<ObjectContent>(
                                                                   objectContentOfTType,
                                                                   type,
                                                                   streamContent);

                               string errorMessage = SR.NoReadSerializerAvailable(typeof(MediaTypeFormatter).Name, type.Name, "application/unknownMediaType");
                               ExceptionAssert.Throws<TargetInvocationException, InvalidOperationException>(
                                   errorMessage,
                                   // wrappingContent.ReadAsObj()
                                   () => GenericTypeAssert.InvokeMethod(wrappingContent, readAsAsyncMethodName));
                           });
                    }
                });
        }

        #endregion ReadAsAsync

        [TestMethod]
        [TestCategory("CIT"), Timeout(TimeoutConstant.DefaultTimeout), Owner("")]
        [Description("ReadAsOrDefault() gets the object instance provided to the constructor for all value and reference types.")]
        public void ReadAsOrDefaultGetsObject()
        {
            TestDataAssert.Execute(
                TestData.RepresentativeValueAndRefTypeTestDataCollection,
                (type, obj) =>
                {
                    ObjectContent content = GenericTypeAssert.InvokeConstructor<ObjectContent>(objectContentOfTType, type, new Type[] { type }, new object[] { obj });
                    object readObj = GenericTypeAssert.InvokeMethod(content, readAsOrDefaultMethodName);
                    Assert.AreEqual(obj, readObj, string.Format("ReadAs failed for type '{0}'.", type.Name));
                });
        }

        [TestMethod]
        [TestCategory("CIT"), Timeout(TimeoutConstant.DefaultTimeout), Owner("")]
        [Description("ReadAsOrDefault() returns default value if no formatters are available.")]
        public void ReadAsOrDefaultReturnsDefaultWithNoFormatter()
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

                                // ObjectContent contentWrappingStream = new ObjectContent<T>(streamContent);
                                ObjectContent wrappingContent = GenericTypeAssert.InvokeConstructor<ObjectContent>(
                                                                    objectContentOfTType,
                                                                    type,
                                                                    streamContent);

                                object readObj = GenericTypeAssert.InvokeMethod(wrappingContent, readAsOrDefaultMethodName);
                                object defaultObj = DefaultValue(type);
                                TestDataAssert.AreEqual(defaultObj, readObj, "Did not read default value.");
                            });
                    }
                });
        }

        [TestMethod]
        [TestCategory("CIT"), Timeout(TimeoutConstant.DefaultTimeout), Owner("")]
        [Description("ReadAsOrDefaultAsync() gets a Task<object> returning object instance provided to the constructor for all value and reference types.")]
        public void ReadAsOrDefaultAsyncGetsTaskOfObject()
        {
            TestDataAssert.Execute(
                TestData.RepresentativeValueAndRefTypeTestDataCollection,
                (type, obj) =>
                {
                    ObjectContent content = GenericTypeAssert.InvokeConstructor<ObjectContent>(objectContentOfTType, type, new Type[] { type }, new object[] { obj });
                    Task task = GenericTypeAssert.InvokeMethod(content, readAsOrDefaultAsyncMethodName) as Task;
                    TaskAssert.ResultEquals(task, obj);
                });
        }

        [TestMethod]
        [TestCategory("CIT"), Timeout(TimeoutConstant.DefaultTimeout), Owner("")]
        [Description("ReadAsOrDefaultAsync() returns Task yielding default value if no formatters are available.")]
        public void ReadAsOrDefaultAsyncReturnsDefaultWithNoFormatter()
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

                                // ObjectContent contentWrappingStream = new ObjectContent<T>(streamContent);
                                ObjectContent wrappingContent = GenericTypeAssert.InvokeConstructor<ObjectContent>(
                                                                    objectContentOfTType,
                                                                    type,
                                                                    streamContent);

                                Task readTask = GenericTypeAssert.InvokeMethod(wrappingContent, readAsOrDefaultAsyncMethodName) as Task;
                                Assert.IsNotNull(readTask, "Should have returned a Task.");
                                object readObj = TaskAssert.SucceedsWithResult(readTask);
                                object defaultObj = DefaultValue(type);
                                TestDataAssert.AreEqual(defaultObj, readObj, "Did not read default value.");
                            });
                    }
                });
        }

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
