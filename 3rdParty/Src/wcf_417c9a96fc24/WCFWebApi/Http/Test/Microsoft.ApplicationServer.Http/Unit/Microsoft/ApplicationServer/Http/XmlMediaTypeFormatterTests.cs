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
    using System.Runtime.Serialization;
    using System.Threading.Tasks;
    using System.Xml.Serialization;
    using Microsoft.ApplicationServer.Common.Test;
    using Microsoft.ApplicationServer.Common.Test.Types;
    using Microsoft.ApplicationServer.Http.Moles;
    using Microsoft.ApplicationServer.Http.Test;
    using Microsoft.VisualStudio.TestTools.UnitTesting;

    [TestClass, UnitTestLevel(UnitTestLevel.InProgress)]
    public class XmlMediaTypeFormatterTests : UnitTest<XmlMediaTypeFormatter>
    {
        [TestCleanup, HostType("Moles")]
        public void TestCleanup()
        {
            // Ensure every test resets Moled object back to default
            MXmlMediaTypeFormatter.BehaveAsCurrent();
        }

        #region Type

        [TestMethod]
        [TestCategory("CIT"), Timeout(TimeoutConstant.DefaultTimeout), Owner("")]
        [Description("XmlMediaTypeFormatter is public, concrete, and unsealed.")]
        public void TypeIsCorrect()
        {
            Type t = this.TypeUnderTest;
            Assert.IsTrue(t.IsPublic, "XmlMediaTypeFormatter should be public.");
            Assert.IsFalse(t.IsAbstract, "XmlMediaTypeFormatter should not be abstract.");
            Assert.IsFalse(t.IsSealed, "XmlMediaTypeFormatter should not be sealed.");
            Assert.AreEqual(typeof(MediaTypeFormatter), this.TypeUnderTest.BaseType, "XmlMediaTypeFormatter should derive from MediaTypeFormatter.");
        }

        #endregion Type

        #region Constructors

        [TestMethod]
        [TestCategory("CIT"), Timeout(TimeoutConstant.DefaultTimeout), Owner("")]
        [Description("XmlMediaTypeFormatter() constructor sets standard Xml media types in SupportedMediaTypes.")]
        public void Constructor()
        {
            XmlMediaTypeFormatter formatter = new XmlMediaTypeFormatter();
            foreach (MediaTypeHeaderValue mediaType in HttpTestData.StandardXmlMediaTypes)
            {
                Assert.IsTrue(formatter.SupportedMediaTypes.Contains(mediaType), string.Format("SupportedMediaTypes should have included {0}.", mediaType.ToString()));
            }
        }

        #endregion Constructors

        #region Properties

        #region DefaultMediaType

        [TestMethod]
        [TestCategory("CIT"), Timeout(TimeoutConstant.DefaultTimeout), Owner("")]
        [Description("DefaultMediaType property retirms application/xml.")]
        public void DefaultMediaTypeReturnsApplicationXml()
        {
            MediaTypeHeaderValue mediaType = XmlMediaTypeFormatter.DefaultMediaType;
            Assert.IsNotNull(mediaType, "DefaultMediaType cannot be null.");
            Assert.AreEqual("application/xml", mediaType.MediaType);
        }

        #endregion DefaultMediaType

        #endregion Properties

        #region Methods

        #region CanReadType

        [Ignore]
        [TestMethod]
        [TestCategory("CIT"), Timeout(TimeoutConstant.ExtendedTimeout), Owner("")]
        [Description("XmlMediaTypeFormatter.CanReadType returns the same result as the XmlSerializer constructor.")]
        public void CanReadTypeReturnsSameResultAsXmlSerializerConstructor()
        {
            TestXmlMediaTypeFormatter formatter = new TestXmlMediaTypeFormatter();
            TestDataAssert.Execute(
                TestData.RepresentativeValueAndRefTypeTestDataCollection,
                (type, obj) =>
                {
                    bool isSerializable = IsSerializableWithXmlSerializer(type, obj);
                    bool canSupport = formatter.CanReadTypeCaller(type);
                    if (isSerializable != canSupport)
                    {
                        Assert.AreEqual(isSerializable, canSupport, string.Format("CanReadType returned wrong value for '{0}'.", type));
                    }

                    // Ask a 2nd time to probe whether the cached result is treated the same
                    canSupport = formatter.CanReadTypeCaller(type);
                    if (isSerializable != canSupport)
                    {
                        Assert.Fail(string.Format("2nd CanReadType returned wrong value for '{0}'.", type));
                    }
                });
        }

        #endregion CanReadType

        #region SetSerializer(Type, XmlSerializer)

        [TestMethod]
        [TestCategory("CIT"), Timeout(TimeoutConstant.DefaultTimeout), Owner("")]
        [Description("XmlMediaTypeFormatter.SetSerializer(Type, XmlSerializer) throws with null type.")]
        public void SetSerializerThrowsWithNullType()
        {
            XmlMediaTypeFormatter formatter = new XmlMediaTypeFormatter();
            XmlSerializer xmlSerializer = new XmlSerializer(typeof(string));
            ExceptionAssert.ThrowsArgumentNull("type", () => { formatter.SetSerializer(null, xmlSerializer); });
        }

        [TestMethod]
        [TestCategory("CIT"), Timeout(TimeoutConstant.DefaultTimeout), Owner("")]
        [Description("XmlMediaTypeFormatter.SetSerializer(Type, XmlSerializer) throws with null serializer.")]
        public void SetSerializerThrowsWithNullSerializer()
        {
            XmlMediaTypeFormatter formatter = new XmlMediaTypeFormatter();
            ExceptionAssert.ThrowsArgumentNull("serializer", () => { formatter.SetSerializer(typeof(string), (XmlSerializer)null); });
        }

        #endregion SetSerializer(Type, XmlSerializer)

        #region SetSerializer<T>(XmlSerializer)

        [TestMethod]
        [TestCategory("CIT"), Timeout(TimeoutConstant.DefaultTimeout), Owner("")]
        [Description("XmlMediaTypeFormatter.SetSerializer<T>(XmlSerializer) throws with null serializer.")]
        public void SetSerializer1ThrowsWithNullSerializer()
        {
            XmlMediaTypeFormatter formatter = new XmlMediaTypeFormatter();
            ExceptionAssert.ThrowsArgumentNull("serializer", () => { formatter.SetSerializer<string>((XmlSerializer)null); });
        }

        #endregion SetSerializer<T>(XmlSerializer)

        #region SetSerializer(Type, XmlObjectSerializer)

        [TestMethod]
        [TestCategory("CIT"), Timeout(TimeoutConstant.DefaultTimeout), Owner("")]
        [Description("XmlMediaTypeFormatter.SetSerializer(Type, XmlObjectSerializer) throws with null type.")]
        public void SetSerializer2ThrowsWithNullType()
        {
            XmlMediaTypeFormatter formatter = new XmlMediaTypeFormatter();
            XmlObjectSerializer xmlObjectSerializer = new DataContractSerializer(typeof(string));
            ExceptionAssert.ThrowsArgumentNull("type", () => { formatter.SetSerializer(null, xmlObjectSerializer); });
        }

        [TestMethod]
        [TestCategory("CIT"), Timeout(TimeoutConstant.DefaultTimeout), Owner("")]
        [Description("XmlMediaTypeFormatter.SetSerializer(Type, XmlObjectSerializer) throws with null serializer.")]
        public void SetSerializer2ThrowsWithNullSerializer()
        {
            XmlMediaTypeFormatter formatter = new XmlMediaTypeFormatter();
            ExceptionAssert.ThrowsArgumentNull("serializer", () => { formatter.SetSerializer(typeof(string), (XmlObjectSerializer)null); });
        }

        #endregion SetSerializer(Type, XmlObjectSerializer)

        #region SetSerializer<T>(XmlObjectSerializer)

        [TestMethod]
        [TestCategory("CIT"), Timeout(TimeoutConstant.DefaultTimeout), Owner("")]
        [Description("XmlMediaTypeFormatter.SetSerializer<T>(XmlObjectSerializer) throws with null serializer.")]
        public void SetSerializer3ThrowsWithNullSerializer()
        {
            XmlMediaTypeFormatter formatter = new XmlMediaTypeFormatter();
            ExceptionAssert.ThrowsArgumentNull("serializer", () => { formatter.SetSerializer<string>((XmlSerializer)null); });
        }

        #endregion SetSerializer<T>(XmlObjectSerializer)

        #region RemoveSerializer()

        [TestMethod]
        [TestCategory("CIT"), Timeout(TimeoutConstant.DefaultTimeout), Owner("")]
        [Description("XmlMediaTypeFormatter.RemoveSerializer throws with null type.")]
        public void RemoveSerializerThrowsWithNullType()
        {
            XmlMediaTypeFormatter formatter = new XmlMediaTypeFormatter();
            ExceptionAssert.ThrowsArgumentNull("type", () => { formatter.RemoveSerializer(null); });
        }

        #endregion RemoveSerializer()

        #region ReadFromStream() using XmlSerializer

        [TestMethod]
        [TestCategory("CIT"), Timeout(TimeoutConstant.ExtendedTimeout), Owner("")]
        [Description("XmlMediaTypeFormatter.ReadFromStream returns all value and reference types serialized via WriteToStream using XmlSerializer.")]
        public void ReadFromStreamRoundTripsWriteToStreamUsingXmlSerializer()
        {
            TestXmlMediaTypeFormatter formatter = new TestXmlMediaTypeFormatter();
            HttpContentHeaders contentHeaders = new StringContent(string.Empty).Headers;

            // Excludes ReferenceDataContractType tests because XmlSerializer cannot handle circular references
            TestDataAssert.Execute(
                TestData.ValueAndRefTypeTestDataCollection.Where((td) => !(typeof(RefTypeTestData<ReferenceDataContractType>).IsAssignableFrom(td.GetType()))),
                (type, obj) =>
                {
                    bool canSerialize = IsSerializableWithXmlSerializer(type, obj) && HttpTestData.CanRoundTrip(type);

                    if (canSerialize)
                    {
                        formatter.SetSerializer(type, new XmlSerializer(type));

                        object readObj = null;
                        StreamAssert.WriteAndRead(
                            (stream) => formatter.WriteToStream(type, obj, stream, contentHeaders, /*transportContext*/ null),
                            (stream) => readObj = formatter.ReadFromStream(type, stream, contentHeaders));
                        TestDataAssert.AreEqual(obj, readObj, "Failed to round trip object");
                    }
                });
        }

        #endregion ReadFromStream() using XmlSerializer

        #region ReadFromStream() using DataContractSerializer

        [TestMethod]
        [TestCategory("CIT"), Timeout(TimeoutConstant.DefaultTimeout), Owner("")]
        [Description("XmlMediaTypeFormatter.ReadFromStream returns all value and reference types serialized via WriteToStream using DataContractSerializer.")]
        public void ReadFromStreamRoundTripsWriteToStreamUsingDataContractSerializer()
        {
            TestXmlMediaTypeFormatter formatter = new TestXmlMediaTypeFormatter();
            HttpContentHeaders contentHeaders = new StringContent(string.Empty).Headers;

            TestDataAssert.Execute(
                TestData.ValueAndRefTypeTestDataCollection,
                (type, obj) =>
                {
                    bool canSerialize = IsSerializableWithDataContractSerializer(type, obj) && HttpTestData.CanRoundTrip(type);
                    if (canSerialize)
                    {
                        formatter.SetSerializer(type, new DataContractSerializer(type));

                        object readObj = null;
                        StreamAssert.WriteAndRead(
                            (stream) => formatter.WriteToStream(type, obj, stream, contentHeaders, /*transportContext*/ null),
                            (stream) => readObj = formatter.ReadFromStream(type, stream, contentHeaders));
                        TestDataAssert.AreEqual(obj, readObj, "Failed to round trip object");
                    }
                });
        }

        #endregion ReadFromStream() using DataContractSerializer

        #region ReadFromStreamAsync() using XmlSerializer

        [TestMethod]
        [TestCategory("CIT"), Timeout(TimeoutConstant.ExtendedTimeout), Owner("")]
        [Description("XmlMediaTypeFormatter.ReadFromStreamAsync returns all value and reference types serialized via WriteToStreamAsync using XmlSerializer.")]
        public void ReadFromStreamAsyncRoundTripsWriteToStreamAsyncUsingXmlSerializer()
        {
            TestXmlMediaTypeFormatter formatter = new TestXmlMediaTypeFormatter();
            HttpContentHeaders contentHeaders = new StringContent(string.Empty).Headers;

            TestDataAssert.Execute(
                TestData.RepresentativeValueAndRefTypeTestDataCollection,
                (type, obj) =>
                {
                    bool canSerialize = IsSerializableWithXmlSerializer(type, obj) && HttpTestData.CanRoundTrip(type);
                    if (canSerialize)
                    {
                        formatter.SetSerializer(type, new XmlSerializer(type));

                        object readObj = null;
                        StreamAssert.WriteAndRead(
                            (stream) => TaskAssert.Succeeds(formatter.WriteToStreamAsync(type, obj, stream, contentHeaders, /*transportContext*/ null)),
                            (stream) => readObj = TaskAssert.SucceedsWithResult(formatter.ReadFromStreamAsync(type, stream, contentHeaders))
                            );
                        TestDataAssert.AreEqual(obj, readObj, "Failed to round trip object");
                    }
                });
        }

        #endregion ReadFromStreamAsync() using XmlSerializer

        #region ReadFromStreamAsync() using DataContractSerializer

        [TestMethod]
        [TestCategory("CIT"), Timeout(TimeoutConstant.DefaultTimeout), Owner("")]
        [Description("XmlMediaTypeFormatter.ReadFromStream returns all value and reference types serialized via WriteToStream using DataContractSerializer.")]
        public void ReadFromStreamAsyncRoundTripsWriteToStreamUsingDataContractSerializer()
        {
            TestXmlMediaTypeFormatter formatter = new TestXmlMediaTypeFormatter();
            HttpContentHeaders contentHeaders = new StringContent(string.Empty).Headers;

            TestDataAssert.Execute(
                TestData.RepresentativeValueAndRefTypeTestDataCollection,
                (type, obj) =>
                {
                    bool canSerialize = IsSerializableWithDataContractSerializer(type, obj) && HttpTestData.CanRoundTrip(type);
                    if (canSerialize)
                    {
                        formatter.SetSerializer(type, new DataContractSerializer(type));

                        object readObj = null;
                        StreamAssert.WriteAndRead(
                            (stream) => TaskAssert.Succeeds(formatter.WriteToStreamAsync(type, obj, stream, contentHeaders, /*transportContext*/ null)),
                            (stream) => readObj = TaskAssert.SucceedsWithResult(formatter.ReadFromStreamAsync(type, stream, contentHeaders))
                            );
                        TestDataAssert.AreEqual(obj, readObj, "Failed to round trip object.");
                    }
                });
        }

        #endregion ReadFromStreamAsync() using DataContractSerializer

        #endregion Methods

        #region Test types

        public class TestXmlMediaTypeFormatter : XmlMediaTypeFormatter
        {
            public bool CanReadTypeCaller(Type type)
            {
                return this.CanReadType(type);
            }
        }

        #endregion Test types

        #region Test helpers

        private static bool IsSerializableWithXmlSerializer(Type type, object obj)
        {
            if (HttpTestData.IsKnownUnserializable(type, obj))
            {
                return false;
            }

            try
            {
                new XmlSerializer(type);
                if (obj != null && obj.GetType() != type)
                {
                    new XmlSerializer(obj.GetType());
                }
            }
            catch
            {
                return false;
            }

            return true;
        }

        private static bool IsSerializableWithDataContractSerializer(Type type, object obj)
        {
            if (HttpTestData.IsKnownUnserializable(type, obj))
            {
                return false;
            }

            try
            {
                new DataContractSerializer(type);
                if (obj != null && obj.GetType() != type)
                {
                    new DataContractSerializer(obj.GetType());
                }
            }
            catch
            {
                return false;
            }

            return true;
        }

        #endregion Test helpers
    }
}
