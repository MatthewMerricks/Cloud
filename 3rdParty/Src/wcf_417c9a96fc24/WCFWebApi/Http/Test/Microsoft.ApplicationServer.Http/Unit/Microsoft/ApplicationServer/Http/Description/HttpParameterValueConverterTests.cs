// <copyright>
//   Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>

namespace Microsoft.ApplicationServer.Http.Description
{
    using System;
    using System.Net.Http;
    using Microsoft.ApplicationServer.Common.Test;
    using Microsoft.ApplicationServer.Http.Description.Moles;
    using Microsoft.ApplicationServer.Http.Test;
    using Microsoft.VisualStudio.TestTools.UnitTesting;

    [TestClass, UnitTestLevel(UnitTestLevel.InProgress), UnitTestType(typeof(HttpParameterValueConverter))]
    public class HttpParameterValueConverterTests : UnitTest
    {
        #region Type

        [TestMethod]
        [TestCategory("CIT"), Timeout(TimeoutConstant.DefaultTimeout), Owner("")]
        [Description("HttpParameverValueConverter is internal, and abstract.")]
        public void TypeIsCorrect()
        {
            Type t = this.TypeUnderTest;
            Assert.IsTrue(t.IsNotPublic, "HttpParameterValueConverter should be internal.");
            Assert.IsTrue(t.IsAbstract, "HttpParameterValueConverter should be abstract");
        }

        #endregion Type

        #region Constructors

        [TestMethod, HostType("Moles")]
        [TestCategory("CIT"), Timeout(TimeoutConstant.DefaultTimeout), Owner("")]
        [Description("HttpParameverValueConverter(Type) sets Type for all value types.")]
        public void Constructor()
        {
            foreach (TestData testData in HttpTestData.ConvertableValueTypes)
            {
                SHttpParameterValueConverter converter = new SHttpParameterValueConverter(testData.Type);
                Assert.IsNotNull(converter.Type, "Converter failed to set Type.");
                Assert.IsTrue(converter.Type.IsAssignableFrom(testData.Type), string.Format("Converter type {0} was not assignable from test data type {1}", converter.Type.Name, testData.Type.Name));
            }
        }

        #endregion Constructors

        #region Properties

        #region CanConvertFromString

        #region CanConvertFromString for value types

        [TestMethod]
        [TestCategory("CIT"), Timeout(TimeoutConstant.DefaultTimeout), Owner("")]
        [Description("CanConvertFromString returns true for all value types converters.")]
        public void CanConvertFromStringReturnsTrueForT()
        {
            foreach (TestData testData in HttpTestData.ConvertableValueTypes)
            {
                HttpParameterValueConverter converter = HttpParameterValueConverter.GetValueConverter(testData.Type);
                if (!converter.CanConvertFromString)
                {
                    Assert.Fail(string.Format("CanConvertFromString was wrong for {0}.", testData.Type.Name));
                }
            }
        }

        #endregion CanConvertFromString for value types

        #region CanConvertFromString for reference types

        [TestMethod]
        [TestCategory("CIT"), Timeout(TimeoutConstant.DefaultTimeout), Owner("")]
        [Description("CanConvertFromString returns false for ObjectContent converters.")]
        public void CanConvertFromStringReturnsFalseForObjectContent()
        {
            ObjectContent objectContent = new ObjectContent<int>(5);
            HttpParameterValueConverter converter = HttpParameterValueConverter.GetValueConverter(objectContent.GetType());
            if (converter.CanConvertFromString)
            {
                Assert.Fail(string.Format("CanConvertFromString was wrong for ObjectContent."));
            }
        }

        [TestMethod]
        [TestCategory("CIT"), Timeout(TimeoutConstant.DefaultTimeout), Owner("")]
        [Description("CanConvertFromString returns false for HttpRequestMessage converters.")]
        public void CanConvertFromStringReturnsFalseForHttpRequestMessage()
        {
            HttpRequestMessage request = new HttpRequestMessage<int>();
            HttpParameterValueConverter converter = HttpParameterValueConverter.GetValueConverter(request.GetType());
            if (converter.CanConvertFromString)
            {
                Assert.Fail(string.Format("CanConvertFromString was wrong for HttpRequestMessage."));
            }
        }

        [TestMethod]
        [TestCategory("CIT"), Timeout(TimeoutConstant.DefaultTimeout), Owner("")]
        [Description("CanConvertFromString returns false for HttpResponseMessage converters.")]
        public void CanConvertFromStringReturnsFalseForHttpResponseMessage()
        {
            HttpResponseMessage response = new HttpResponseMessage<int>(5);
            HttpParameterValueConverter converter = HttpParameterValueConverter.GetValueConverter(response.GetType());
            if (converter.CanConvertFromString)
            {
                Assert.Fail(string.Format("CanConvertFromString was wrong for HttpResponseMessage."));
            }
        }

        #endregion CanConvertFromString for reference types

        #endregion CanConvertFromString

        #endregion Properties

        #region Methods

        #region GetValueConverter()

        [TestMethod]
        [TestCategory("CIT"), Timeout(TimeoutConstant.DefaultTimeout), Owner("")]
        [Description("GetValueConverter(Type) throws with null type.")]
        public void GetValueConverterThrowsWithNullType()
        {
            ExceptionAssert.ThrowsArgumentNull("type", () => HttpParameterValueConverter.GetValueConverter(null));
        }

        [TestMethod]
        [TestCategory("CIT"), Timeout(TimeoutConstant.DefaultTimeout), Owner("")]
        [Description("GetValueConverter(Type) returns a converter for all values type.")]
        public void GetValueConverterReturnsConverterForAllValueTypes()
        {
            TestDataAssert.Execute(
                HttpTestData.ConvertableValueTypes,
                TestDataVariations.AllSingleInstances | TestDataVariations.AsNullable,
                "GetValueConverter failed",
                (type, obj) =>
                {
                    Type convertType = obj.GetType();
                    HttpParameterValueConverter converter = HttpParameterValueConverter.GetValueConverter(convertType);
                    Assert.IsNotNull(converter, "GetValueConverter returned null.");
                    Assert.AreEqual(convertType, converter.Type, "Converter Type was not correct.");
                });
        }

        [TestMethod]
        [TestCategory("CIT"), Timeout(TimeoutConstant.DefaultTimeout), Owner("")]
        [Description("GetValueConverter(Type) returns a converter for all HttpContent types.")]
        public void GetValueConverterHttpContent()
        {
            foreach (HttpContent httpContent in HttpTestData.StandardHttpContents)
            {
                HttpParameterValueConverter converter = HttpParameterValueConverter.GetValueConverter(httpContent.GetType());
                Assert.IsNotNull(converter, "GetValueConverter returned null.");
                Assert.AreEqual(httpContent.GetType(), converter.Type, "Converter Type was not correct.");
            }
        }


        [TestMethod]
        [TestCategory("CIT"), Timeout(TimeoutConstant.DefaultTimeout), Owner("")]
        [Description("GetValueConverter(Type) returns a converter for all ObjectContent<T> types.")]
        public void GetValueConverterObjectContentOfT()
        {
            ObjectContentAssert.ExecuteForEachObjectContent(
                HttpTestData.RepresentativeValueAndRefTypeTestDataCollection,
                TestDataVariations.All,
                (objectContent, type, obj) =>
                {
                    HttpParameterValueConverter converter = HttpParameterValueConverter.GetValueConverter(objectContent.GetType());
                    Assert.AreEqual(objectContent.GetType(), converter.Type, "Converter Type is wrong.");
                });
        }

        [TestMethod]
        [TestCategory("CIT"), Timeout(TimeoutConstant.DefaultTimeout), Owner("")]
        [Description("GetValueConverter(Type) returns a converter for all HttpRequestMessage<T> types.")]
        public void GetValueConverterHttpRequestMessageOfT()
        {
            ObjectContentAssert.ExecuteForEachHttpRequestMessage(
                HttpTestData.RepresentativeValueAndRefTypeTestDataCollection,
                TestDataVariations.All,
                (request, type, obj) =>
                {
                    HttpParameterValueConverter converter = HttpParameterValueConverter.GetValueConverter(request.GetType());
                    Assert.AreEqual(request.GetType(), converter.Type, "Converter Type is wrong.");
                });
        }

        [TestMethod]
        [TestCategory("CIT"), Timeout(TimeoutConstant.DefaultTimeout), Owner("")]
        [Description("GetValueConverter(Type) returns a converter for all HttpResponseMessage<T> types.")]
        public void GetValueConverterHttpResponseMessageOfT()
        {
            ObjectContentAssert.ExecuteForEachHttpResponseMessage(
                HttpTestData.RepresentativeValueAndRefTypeTestDataCollection,
                TestDataVariations.All,
                (response, type, obj) =>
                {
                    HttpParameterValueConverter converter = HttpParameterValueConverter.GetValueConverter(response.GetType());
                    Assert.AreEqual(response.GetType(), converter.Type, "Converter Type is wrong.");
                });
        }

        #endregion GetValueConverter()

        #region CanConvertFromType()

        #region CanConvertFromType(Type) for value types

        [TestMethod]
        [TestCategory("CIT"), Timeout(TimeoutConstant.DefaultTimeout), Owner("")]
        [Description("CanConvertFromType(Type) true for all value types' own types.")]
        public void CanConvertFromTypeReturnsTrueForTtoT()
        {
            foreach (TestData testData in HttpTestData.ConvertableValueTypes)
            {
                HttpParameterValueConverter converter = HttpParameterValueConverter.GetValueConverter(testData.Type);
                if (!converter.CanConvertFromType(testData.Type))
                {
                    Assert.Fail(string.Format("CanConvertFromType was wrong for {0}.", testData.Type.Name));
                }
            }
        }

        #endregion CanConvertFromType(Type) for value types

        #region CanConvertFrom(Type) using ObjectContent<T>

        [TestMethod]
        [TestCategory("CIT"), Timeout(TimeoutConstant.DefaultTimeout), Owner("")]
        [Description("CanConvertFromType(Type) returns false for T to ObjectContent<T>.")]
        public void CanConvertFromTypeReturnsFalseForTtoObjectContentOfT()
        {
            ObjectContentAssert.ExecuteForEachObjectContent(
                HttpTestData.RepresentativeValueAndRefTypeTestDataCollection,
                TestDataVariations.All,
                (objectContent, type, obj) =>
                {
                    HttpParameterValueConverter converter = HttpParameterValueConverter.GetValueConverter(objectContent.GetType());
                    if (converter.CanConvertFromType(objectContent.Type))
                    {
                        Assert.Fail(string.Format("CanConvertFromType failed for {0}.", type));
                    }
                });
        }

        [TestMethod]
        [TestCategory("CIT"), Timeout(TimeoutConstant.DefaultTimeout), Owner("")]
        [Description("CanConvertFromType(Type) throws for null Type using ObjectContent<T>.")]
        public void CanConvertFromTypeThrowsWithNullTypeForTtoObjectContentOfT()
        {
            ObjectContent objectContent = new ObjectContent<int>(5);
            HttpParameterValueConverter converter = HttpParameterValueConverter.GetValueConverter(objectContent.GetType());
            ExceptionAssert.ThrowsArgumentNull("type", () => converter.CanConvertFromType(null));
        }

        [TestMethod]
        [TestCategory("CIT"), Timeout(TimeoutConstant.DefaultTimeout), Owner("")]
        [Description("CanConvertFromType(Type) returns true for ObjectContent<T> to T.")]
        public void CanConvertFromTypeReturnsTrueForObjectContentOfTtoT()
        {
            ObjectContentAssert.ExecuteForEachObjectContent(
                HttpTestData.RepresentativeValueAndRefTypeTestDataCollection,
                TestDataVariations.All,
                (objectContent, type, obj) =>
                {
                    HttpParameterValueConverter converter = HttpParameterValueConverter.GetValueConverter(objectContent.Type);
                    if (!converter.CanConvertFromType(objectContent.GetType()))
                    {
                        Assert.Fail(string.Format("CanConvertFromType failed for {0}.", objectContent.Type));
                    }
                });
        }

        #endregion CanConvertFrom(Type) using ObjectContent<T>

        #region CanConvertFromType(Type) using HttpRequestMessage<T>

        [TestMethod]
        [TestCategory("CIT"), Timeout(TimeoutConstant.DefaultTimeout), Owner("")]
        [Description("CanConvertFromType(Type) returns false for T to HttpRequestMessage<T>.")]
        public void CanConvertFromTypeReturnsFalseForTtoHttpRequestMessageOfT()
        {
            ObjectContentAssert.ExecuteForEachHttpRequestMessage(
                HttpTestData.RepresentativeValueAndRefTypeTestDataCollection,
                TestDataVariations.All,
                (request, type, obj) =>
                {
                    Type convertType = obj == null ? type : obj.GetType();
                    HttpParameterValueConverter converter = HttpParameterValueConverter.GetValueConverter(request.GetType());
                    if (converter.CanConvertFromType(convertType))
                    {
                        Assert.Fail(string.Format("CanConvertFromType failed for {0}.", convertType));
                    }
                });
        }

        [TestMethod]
        [TestCategory("CIT"), Timeout(TimeoutConstant.DefaultTimeout), Owner("")]
        [Description("CanConvertFromType(Type) returns true for HttpRequesteMessage<T> to T.")]
        public void CanConvertFromTypeReturnsTrueForHttpRequestMessageOfTtoT()
        {
            ObjectContentAssert.ExecuteForEachHttpRequestMessage(
                HttpTestData.RepresentativeValueAndRefTypeTestDataCollection,
                TestDataVariations.All,
                (request, type, obj) =>
                {
                    Type convertType = obj == null ? type : obj.GetType();
                    HttpParameterValueConverter converter = HttpParameterValueConverter.GetValueConverter(convertType);
                    if (!converter.CanConvertFromType(request.GetType()))
                    {
                        Assert.Fail(string.Format("CanConvertFromType failed for {0}.", convertType));
                    }
                });
        }

        #endregion CanConvertFromType(Type) using HttpRequestMessage<T>

        #region CanConvertFromType(Type) using HttpResponseMessage<T>

        [TestMethod]
        [TestCategory("CIT"), Timeout(TimeoutConstant.DefaultTimeout), Owner("")]
        [Description("CanConvertFromType(Type) returns false for T to HttpResponseMessage<T>.")]
        public void CanConvertFromTypeReturnsFalseForTtoHttpResponseMessageOfT()
        {
            ObjectContentAssert.ExecuteForEachHttpResponseMessage(
               HttpTestData.RepresentativeValueAndRefTypeTestDataCollection,
               TestDataVariations.All,
               (response, type, obj) =>
               {
                   Type convertType = obj == null ? type : obj.GetType();
                   HttpParameterValueConverter converter = HttpParameterValueConverter.GetValueConverter(response.GetType());
                    if (converter.CanConvertFromType(convertType))
                    {
                        Assert.Fail(string.Format("CanConvertFromType failed for {0}.", convertType));
                    }
                });
        }

        [TestMethod]
        [TestCategory("CIT"), Timeout(TimeoutConstant.DefaultTimeout), Owner("")]
        [Description("CanConvertFromType(Type) returns true for HttpResponseMessage<T> to T.")]
        public void CanConvertFromTypeReturnsTrueForHttpResponseMessageOfTtoT()
        {
            ObjectContentAssert.ExecuteForEachHttpResponseMessage(
             HttpTestData.RepresentativeValueAndRefTypeTestDataCollection,
             TestDataVariations.All,
             (response, type, obj) =>
             {
                 Type convertType = obj == null ? type : obj.GetType();
                 HttpParameterValueConverter converter = HttpParameterValueConverter.GetValueConverter(convertType);
                 if (!converter.CanConvertFromType(response.GetType()))
                 {
                     Assert.Fail(string.Format("CanConvertFromType failed for {0}.", convertType));
                 }
             });
        }

        #endregion CanConvertFromType(Type) using HttpResponseMessage<T>

        #endregion CanConvertFromType()

        #region Convert(object)

        #region Convert(object) for value types

        [TestMethod]
        [TestCategory("CIT"), Timeout(TimeoutConstant.DefaultTimeout), Owner("")]
        [Description("Convert(object) T to T.")]
        public void ConvertTtoT()
        {
            TestDataAssert.Execute(
                HttpTestData.ConvertableValueTypes,
                (type, obj) =>
                {
                    Type convertType = obj == null ? type : obj.GetType();
                    HttpParameterValueConverter converter = HttpParameterValueConverter.GetValueConverter(convertType); 
                    object actualObj = converter.Convert(obj);
                    TestDataAssert.AreEqual(obj, actualObj, string.Format("Conversion failed for {0}.", convertType.Name));
                });
        }

        [TestMethod]
        [TestCategory("CIT"), Timeout(TimeoutConstant.DefaultTimeout), Owner("")]
        [Description("Convert(object) Nullable<T> to T.")]
        public void ConvertNullableOfTtoT()
        {
            TestDataAssert.Execute(
                HttpTestData.ConvertableValueTypes,
                TestDataVariations.AsNullable,
                "Nullable<T> to T failed.",
                (type, obj) =>
                {
                    Type nonNullableType = obj.GetType();
                    Assert.IsNull(Nullable.GetUnderlyingType(nonNullableType), "Test error: did not expect nullable object instance.");
                    Assert.AreEqual(nonNullableType, Nullable.GetUnderlyingType(type), "Test error: expected only nullable types.");
                    HttpParameterValueConverter converter = HttpParameterValueConverter.GetValueConverter(type);
                    object actualValue = converter.Convert(obj);
                    TestDataAssert.AreEqual(obj, actualValue, "Convert failed on Nullable<T> to T.");
                });
        }

        #endregion Convert(object) for value types

        #region Convert(object) using ObjectContent<T>

        [TestMethod]
        [TestCategory("CIT"), Timeout(TimeoutConstant.DefaultTimeout), Owner("")]
        [Description("Convert(object) throws converting T to ObjectContent<T>.")]
        public void ConvertThrowsWithTtoObjectContentOfT()
        {
            ObjectContentAssert.ExecuteForEachObjectContent(
                HttpTestData.RepresentativeValueAndRefTypeTestDataCollection,
                TestDataVariations.All,
                (objectContent, type, obj) =>
                {
                    if (obj != null)
                    {
                        Type convertType = obj.GetType();
                        HttpParameterValueConverter converter = HttpParameterValueConverter.GetValueConverter(objectContent.GetType());
                        string errorMessage = SR.ValueConversionFailed(convertType.FullName, converter.Type.FullName);
                        ExceptionAssert.Throws<InvalidOperationException>(
                            errorMessage,
                            () => converter.Convert(obj));
                    }
                });
        }

        [TestMethod]
        [TestCategory("CIT"), Timeout(TimeoutConstant.DefaultTimeout), Owner("")]
        [Description("Convert(object) ObjectContent<T> to T.")]
        public void ConvertObjectContentOfTtoT()
        {
            ObjectContentAssert.ExecuteForEachObjectContent(
                HttpTestData.RepresentativeValueAndRefTypeTestDataCollection,
                TestDataVariations.All,
                (objectContent, type, obj) =>
                {
                    Type convertType = obj == null ? type : obj.GetType();
                    HttpParameterValueConverter converter = HttpParameterValueConverter.GetValueConverter(convertType);
                    object actualValue = converter.Convert(objectContent);
                    TestDataAssert.AreEqual(obj, actualValue, "Convert failed to return T from ObjectContent<T>.");

                });
        }

        [TestMethod]
        [TestCategory("CIT"), Timeout(TimeoutConstant.DefaultTimeout), Owner("")]
        [Description("Convert(object) ObjectContent<Nullable<T>> to T.")]
        public void ConvertObjectContentOfNullableOfTtoT()
        {
            TestDataAssert.Execute(
                HttpTestData.ConvertableValueTypes,
                TestDataVariations.AsNullable,
                "ObjectContent<Nullable<T>> failied.",
                (type, obj) =>
                {
                    Type nonNullableType = obj.GetType();
                    Assert.IsNull(Nullable.GetUnderlyingType(nonNullableType), "Test error: did not expect nullable object instance.");
                    Assert.AreEqual(nonNullableType, Nullable.GetUnderlyingType(type), "Test error: expected only nullable types.");

                    ObjectContent objectContent =
                        (ObjectContent)GenericTypeAssert.InvokeConstructor(
                            typeof(ObjectContent<>),
                            type,
                            new Type[] { type },
                            new object[] { obj });

                    HttpParameterValueConverter converter = HttpParameterValueConverter.GetValueConverter(nonNullableType);
                    object actualValue = converter.Convert(objectContent);
                    TestDataAssert.AreEqual(obj, actualValue, "Convert failed to return T from ObjectContent<T>.");
                });
        }

        #endregion Convert(object) using ObjectContent<T>

        #region Convert(object) using HttpRequestMessage<T>

        [TestMethod]
        [TestCategory("CIT"), Timeout(TimeoutConstant.DefaultTimeout), Owner("")]
        [Description("Convert(object) throws converting T to HttpRequestMessage<T>).")]
        public void ConvertThrowsWithTtoHttpRequestMessageOfT()
        {
            ObjectContentAssert.ExecuteForEachHttpRequestMessage(
                HttpTestData.RepresentativeValueAndRefTypeTestDataCollection,
                TestDataVariations.All,
                (request, type, obj) =>
                {
                    if (obj != null)
                    {
                    Type convertType = obj.GetType();
                    HttpParameterValueConverter converter = HttpParameterValueConverter.GetValueConverter(request.GetType());
                    string errorMessage = SR.ValueConversionFailed(convertType.FullName, converter.Type.FullName);

                        ExceptionAssert.Throws<InvalidOperationException>(
                            errorMessage,
                            () => converter.Convert(obj));
                    }
                });
        }

        [TestMethod]
        [TestCategory("CIT"), Timeout(TimeoutConstant.DefaultTimeout), Owner("")]
        [Description("Convert(object) HttpRequestMessage<T> to T.")]
        public void ConvertHttpRequestMessageOfTtoT()
        {
            ObjectContentAssert.ExecuteForEachHttpRequestMessage(
                HttpTestData.RepresentativeValueAndRefTypeTestDataCollection,
                TestDataVariations.All,
                (request, type, obj) =>
                {
                    Type convertType = obj == null ? type : obj.GetType();
                    HttpParameterValueConverter converter = HttpParameterValueConverter.GetValueConverter(convertType);
                    object actualValue = converter.Convert(request);
                    TestDataAssert.AreEqual(obj, actualValue, string.Format("Convert from HttpRequestMessage<T> to T failed for {0}.", convertType));
                });
        }

        [TestMethod]
        [TestCategory("CIT"), Timeout(TimeoutConstant.DefaultTimeout), Owner("")]
        [Description("Convert(object) HttpRequestMessage<T> to ObjectContent<T>.")]
        public void ConvertHttpRequestMessageOfTtoObjectContentOfT()
        {
            ObjectContentAssert.ExecuteForEachHttpRequestMessage(
                HttpTestData.RepresentativeValueAndRefTypeTestDataCollection,
                TestDataVariations.All,
                (request, type, obj) =>
                {
                    Type convertType = obj == null ? type : obj.GetType();
                    ObjectContent objectContent = (ObjectContent)GenericTypeAssert.InvokeConstructor(typeof(ObjectContent<>), convertType, new Type[] { convertType }, new object[] { obj });
                    HttpParameterValueConverter converter = HttpParameterValueConverter.GetValueConverter(objectContent.GetType());
                    ObjectContent convertedContent = converter.Convert(request) as ObjectContent;
                    Assert.IsNotNull(convertedContent, "Failed to convert to ObjectContent.");
                    Assert.AreEqual(((ObjectContent)request.Content).ReadAs(), convertedContent.ReadAs(), "Incorrect value.");
                });
        }

        [TestMethod]
        [TestCategory("CIT"), Timeout(TimeoutConstant.DefaultTimeout), Owner("")]
        [Description("Convert(object) HttpRequestMessage<Nullable<T>> to T.")]
        public void ConvertHttpRequestMessageOfNullableOfTtoT()
        {
            TestDataAssert.Execute(
                HttpTestData.ConvertableValueTypes,
                TestDataVariations.AsNullable,
                "HttpRequestMessage<Nullable<T>> failied.",
                (type, obj) =>
                {
                    Type nonNullableType = obj.GetType();
                    Assert.IsNull(Nullable.GetUnderlyingType(nonNullableType), "Test error: did not expect nullable object instance.");
                    Assert.AreEqual(nonNullableType, Nullable.GetUnderlyingType(type), "Test error: expected only nullable types.");

                    HttpRequestMessage request =
                        (HttpRequestMessage)GenericTypeAssert.InvokeConstructor(
                            typeof(HttpRequestMessage<>),
                            type,
                            new Type[] { type },
                            new object[] { obj });

                    HttpParameterValueConverter converter = HttpParameterValueConverter.GetValueConverter(nonNullableType);
                    object actualValue = converter.Convert(request);
                    TestDataAssert.AreEqual(obj, actualValue, "Convert failed to return T from HttpRequestMessage<T>.");
                });
        }

        #endregion Convert(object) using HttpRequestMessage<T>

        #region Convert(object) using HttpResponseMessage<T>

        [TestMethod]
        [TestCategory("CIT"), Timeout(TimeoutConstant.DefaultTimeout), Owner("")]
        [Description("Convert(object) throws converting from T to HttpResponseMessage<T>).")]
        public void ConvertThrowsWithTtoHttpResponseMessageOfT()
        {
            ObjectContentAssert.ExecuteForEachHttpResponseMessage(
                HttpTestData.RepresentativeValueAndRefTypeTestDataCollection,
                TestDataVariations.All,
                (response, type, obj) =>
                {
                    if (obj != null)
                    {
                        Type convertType = obj.GetType();
                        HttpParameterValueConverter converter = HttpParameterValueConverter.GetValueConverter(response.GetType());
                        string errorMessage = SR.ValueConversionFailed(convertType.FullName, converter.Type.FullName);
                        ExceptionAssert.Throws<InvalidOperationException>(
                            errorMessage,
                            () => converter.Convert(obj));
                    }
                });
        }

        [TestMethod]
        [TestCategory("CIT"), Timeout(TimeoutConstant.DefaultTimeout), Owner("")]
        [Description("Convert(object) HttpResponseMessage<T> to T.")]
        public void ConvertHttpResponseMessageOfTtoT()
        {
            ObjectContentAssert.ExecuteForEachHttpResponseMessage(
                HttpTestData.RepresentativeValueAndRefTypeTestDataCollection,
                TestDataVariations.All,
                (response, type, obj) =>
                {
                    Type convertType = obj == null ? type : obj.GetType();
                    HttpParameterValueConverter converter = HttpParameterValueConverter.GetValueConverter(convertType);
                    object actualValue = converter.Convert(response);
                    TestDataAssert.AreEqual(obj, actualValue, string.Format("Convert from HttpResponseMessage<T> to T failed for {0}.", convertType));
                });
        }

        [TestMethod]
        [TestCategory("CIT"), Timeout(TimeoutConstant.DefaultTimeout), Owner("")]
        [Description("Convert(object) HttpResponseMessage<T> to ObjectContent<T>.")]
        public void ConvertHttpResponseMessageOfTtoObjectContentOfT()
        {
            ObjectContentAssert.ExecuteForEachHttpResponseMessage(
                HttpTestData.RepresentativeValueAndRefTypeTestDataCollection,
                TestDataVariations.All,
                (response, type, obj) =>
                {
                    Type convertType = obj == null ? type : obj.GetType();
                    ObjectContent objectContent = (ObjectContent)GenericTypeAssert.InvokeConstructor(typeof(ObjectContent<>), convertType, new Type[] { convertType }, new object[] { obj });
                    HttpParameterValueConverter converter = HttpParameterValueConverter.GetValueConverter(objectContent.GetType());
                    ObjectContent convertedContent = converter.Convert(response) as ObjectContent;
                    Assert.IsNotNull(convertedContent, "Failed to convert to ObjectContent.");
                    Assert.AreEqual(((ObjectContent)response.Content).ReadAs(), convertedContent.ReadAs(), "Incorrect value.");
                });
        }

        [TestMethod]
        [TestCategory("CIT"), Timeout(TimeoutConstant.DefaultTimeout), Owner("")]
        [Description("Convert(object) HttpResponseMessage<Nullable<T>> to T.")]
        public void ConvertHttpResponseMessageOfNullableTtoT()
        {
            TestDataAssert.Execute(
                HttpTestData.ConvertableValueTypes,
                TestDataVariations.AsNullable,
                "HttpResponseMessage<Nullable<T>> failied.",
                (type, obj) =>
                {
                    Type nonNullableType = obj.GetType();
                    Assert.IsNull(Nullable.GetUnderlyingType(nonNullableType), "Test error: did not expect nullable object instance.");
                    Assert.AreEqual(nonNullableType, Nullable.GetUnderlyingType(type), "Test error: expected only nullable types.");

                    HttpResponseMessage request =
                        (HttpResponseMessage)GenericTypeAssert.InvokeConstructor(
                            typeof(HttpResponseMessage<>),
                            type,
                            new Type[] { type },
                            new object[] { obj });

                    HttpParameterValueConverter converter = HttpParameterValueConverter.GetValueConverter(nonNullableType);
                    object actualValue = converter.Convert(request);
                    TestDataAssert.AreEqual(obj, actualValue, "Convert failed to return T from HttpReesponseMessage<T>.");
                });
        }


        #endregion Convert(object) using HttpResponseMessage<T>

        #endregion Convert(object)

        #region Convert(string)

        #region Convert(string) for value types

        [TestMethod]
        [TestCategory("CIT"), Timeout(TimeoutConstant.DefaultTimeout), Owner("")]
        [Description("Convert(string) to T.")]
        public void ConvertStringToT()
        {
            TestDataAssert.Execute(
                HttpTestData.ConvertableValueTypes,
                TestDataVariations.AllSingleInstances,
                "Convert(string) failed",
                (type, obj) =>
                {
                    Type convertType = obj == null ? type : obj.GetType();

                    if (HttpParameterAssert.CanConvertToStringAndBack(obj))
                    {
                        HttpParameterValueConverter converter = HttpParameterValueConverter.GetValueConverter(convertType);
                        string objAsString = obj.ToString();
                        object actualObj = converter.Convert(objAsString);
                        Assert.IsNotNull(actualObj, "Convert from string returned null.");
                        Assert.AreEqual(obj.GetType(), actualObj.GetType(), "Convert from string returned wrong type.");
                        string actualObjAsString = actualObj.ToString();
                        Assert.AreEqual(objAsString, actualObjAsString, string.Format("Conversion failed for {0}.", convertType.Name));
                    }
                });
        }

        #endregion Convert(string) for value types

        #region Convert(string) for reference types

        [TestMethod]
        [TestCategory("CIT"), Timeout(TimeoutConstant.DefaultTimeout), Owner("")]
        [Description("Convert(string) to ObjectContent<T> throws.")]
        public void ConvertStringToObjectContentOfTThrows()
        {
            ObjectContentAssert.ExecuteForEachObjectContent(
                HttpTestData.ConvertableValueTypes,
                TestDataVariations.AllSingleInstances,
                (objectContent, type, obj) =>
                {
                    Type convertType = obj == null ? type : obj.GetType();
                    HttpParameterValueConverter converter = HttpParameterValueConverter.GetValueConverter(objectContent.GetType());
                    string errorMessage = SR.ValueConversionFailed(typeof(string).FullName, converter.Type.FullName);
                    ExceptionAssert.Throws<InvalidOperationException>(
                        errorMessage,
                        () => converter.Convert("random string"));
                });
        }

        [TestMethod]
        [TestCategory("CIT"), Timeout(TimeoutConstant.DefaultTimeout), Owner("")]
        [Description("Convert(string) to HttpRequestMessage<T> throws.")]
        public void ConvertStringToHttpRequestMessageOfTThrows()
        {
            ObjectContentAssert.ExecuteForEachHttpRequestMessage(
                HttpTestData.ConvertableValueTypes,
                TestDataVariations.AllSingleInstances,
                (request, type, obj) =>
                {
                    Type convertType = obj == null ? type : obj.GetType();
                    HttpParameterValueConverter converter = HttpParameterValueConverter.GetValueConverter(request.GetType());
                    string errorMessage = SR.ValueConversionFailed(typeof(string).FullName, converter.Type.FullName);
                    ExceptionAssert.Throws<InvalidOperationException>(
                        errorMessage,
                        () => converter.Convert("random string"));
                });
        }

        [TestMethod]
        [TestCategory("CIT"), Timeout(TimeoutConstant.DefaultTimeout), Owner("")]
        [Description("Convert(string) to HttpResponseMessage<T> throws.")]
        public void ConvertStringToHttpResponseMessageOfTThrows()
        {
            ObjectContentAssert.ExecuteForEachHttpResponseMessage(
                HttpTestData.ConvertableValueTypes,
                TestDataVariations.AllSingleInstances,
                (response, type, obj) =>
                {
                    Type convertType = obj == null ? type : obj.GetType();
                    HttpParameterValueConverter converter = HttpParameterValueConverter.GetValueConverter(response.GetType());
                    string errorMessage = SR.ValueConversionFailed(typeof(string).FullName, converter.Type.FullName);
                    ExceptionAssert.Throws<InvalidOperationException>(
                        errorMessage,
                        () => converter.Convert("random string"));
                });
        }

        #endregion Convert(string) for reference types

        #endregion Convert(string)

        #endregion Methods
    }
}
