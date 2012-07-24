// <copyright>
//   Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>

namespace Microsoft.ApplicationServer.Http
{
    using System;
    using System.IO;
    using System.Net.Http;
    using System.Net.Http.Headers;
    using System.Runtime.Serialization.Json;
    using System.Threading.Tasks;
    using Microsoft.ApplicationServer.Common.Test;
    using Microsoft.ApplicationServer.Common.Test.Types;
    using Microsoft.ApplicationServer.Http.Moles;
    using Microsoft.ApplicationServer.Http.Test;
    using Microsoft.VisualStudio.TestTools.UnitTesting;

    [TestClass, UnitTestLevel(UnitTestLevel.InProgress)]
    public class JsonMediaTypeFormatterTests : UnitTest<JsonMediaTypeFormatter>
    {
        [TestCleanup, HostType("Moles")]
        public void TestCleanup()
        {
            // Ensure every test resets Moled object back to default
            MJsonMediaTypeFormatter.BehaveAsCurrent();
        }

        #region Type

        [TestMethod]
        [TestCategory("CIT"), Timeout(TimeoutConstant.DefaultTimeout), Owner("")]
        [Description("JsonMediaTypeFormatter is public, concrete, and unsealed.")]
        public void TypeIsCorrect()
        {
            Type t = this.TypeUnderTest;
            Assert.IsTrue(t.IsPublic, "JsonMediaTypeFormatter should be public.");
            Assert.IsFalse(t.IsAbstract, "JsonMediaTypeFormatter should not be abstract.");
            Assert.IsFalse(t.IsSealed, "JsonMediaTypeFormatter should not be sealed.");
            Assert.AreEqual(typeof(MediaTypeFormatter), this.TypeUnderTest.BaseType, "JsonMediaTypeFormatter should derive from MediaTypeFormatter.");
        }

        #endregion Type

        #region Constructors

        [TestMethod]
        [TestCategory("CIT"), Timeout(TimeoutConstant.DefaultTimeout), Owner("")]
        [Description("JsonMediaTypeFormatter() constructor sets standard Json media types in SupportedMediaTypes.")]
        public void Constructor()
        {
            JsonMediaTypeFormatter formatter = new JsonMediaTypeFormatter();
            foreach (MediaTypeHeaderValue mediaType in HttpTestData.StandardJsonMediaTypes)
            {
                Assert.IsTrue(formatter.SupportedMediaTypes.Contains(mediaType), string.Format("SupportedMediaTypes should have included {0}.", mediaType.ToString()));
            }
        }

        #endregion Constructors

        #region Properties

        #region DefaultMediaType

        [TestMethod]
        [TestCategory("CIT"), Timeout(TimeoutConstant.DefaultTimeout), Owner("")]
        [Description("DefaultMediaType property returns application/json.")]
        public void DefaultMediaTypeReturnsApplicationJson()
        {
            MediaTypeHeaderValue mediaType = JsonMediaTypeFormatter.DefaultMediaType;
            Assert.IsNotNull(mediaType, "DefaultMediaType cannot be null.");
            Assert.AreEqual("application/json", mediaType.MediaType);
        }

        #endregion DefaultMediaType

        #endregion Properties

        #region Methods

        #region CanReadType()

        [TestMethod]
        [TestCategory("CIT"), Timeout(TimeoutConstant.DefaultTimeout), Owner("")]
        [Description("JsonMediaTypeFormatter.CanReadType returns the expected results for all known value and reference types.")]
        public void CanReadTypeReturnsExpectedValues()
        {
            TestJsonMediaTypeFormatter formatter = new TestJsonMediaTypeFormatter();
            TestDataAssert.Execute(
                TestData.RepresentativeValueAndRefTypeTestDataCollection,
                (type, obj) =>
                {
                    bool isSerializable = IsTypeSerializableWithJsonSerializer(type, obj);
                    bool canSupport = formatter.CanReadTypeProxy(type);

                    // If we don't agree, we assert only if the DCJ serializer says it cannot support something we think it should
                    if (isSerializable != canSupport && isSerializable)
                    {
                        Assert.Fail(string.Format("CanReadType returned wrong value for '{0}'.", type));
                    }

                    // Ask a 2nd time to probe whether the cached result is treated the same
                    canSupport = formatter.CanReadTypeProxy(type);
                    if (isSerializable != canSupport && isSerializable)
                    {
                        Assert.Fail(string.Format("2nd CanReadType returned wrong value for '{0}'.", type));
                    }
                });
        }

        #endregion CanReadType()

        #region SetSerializer()

        [TestMethod]
        [TestCategory("CIT"), Timeout(TimeoutConstant.DefaultTimeout), Owner("")]
        [Description("JsonMediaTypeFormatter.SetSerializer throws with null type.")]
        public void SetSerializerThrowsWithNullType()
        {
            JsonMediaTypeFormatter formatter = new JsonMediaTypeFormatter();
            DataContractJsonSerializer serializer = new DataContractJsonSerializer(typeof(string));
            ExceptionAssert.ThrowsArgumentNull("type", () => { formatter.SetSerializer(null, serializer); });
        }

        [TestMethod]
        [TestCategory("CIT"), Timeout(TimeoutConstant.DefaultTimeout), Owner("")]
        [Description("JsonMediaTypeFormatter.SetSerializer throws with null serializer.")]
        public void SetSerializerThrowsWithNullSerializer()
        {
            JsonMediaTypeFormatter formatter = new JsonMediaTypeFormatter();
            ExceptionAssert.ThrowsArgumentNull("serializer", () => { formatter.SetSerializer(typeof(string), null); });
        }

        #endregion SetSerializer()

        #region SetSerializer<T>()

        [TestMethod]
        [TestCategory("CIT"), Timeout(TimeoutConstant.DefaultTimeout), Owner("")]
        [Description("JsonMediaTypeFormatter.SetSerializer<T>() throws with null serializer.")]
        public void SetSerializer1ThrowsWithNullSerializer()
        {
            JsonMediaTypeFormatter formatter = new JsonMediaTypeFormatter();
            ExceptionAssert.ThrowsArgumentNull("serializer", () => { formatter.SetSerializer<string>(null); });
        }

        #endregion SetSerializer<T>()

        #region RemoveSerializer()

        [TestMethod]
        [TestCategory("CIT"), Timeout(TimeoutConstant.DefaultTimeout), Owner("")]
        [Description("JsonMediaTypeFormatter.RemoveSerializer throws with null type.")]
        public void RemoveSerializerThrowsWithNullType()
        {
            JsonMediaTypeFormatter formatter = new JsonMediaTypeFormatter();
            ExceptionAssert.ThrowsArgumentNull("type", () => { formatter.RemoveSerializer(null); });
        }

        #endregion RemoveSerializer()

        #region ReadFromStream()

        [TestMethod]
        [TestCategory("CIT"), Timeout(TimeoutConstant.DefaultTimeout), Owner("")]
        [Description("JsonMediaTypeFormatter.ReadFromStream returns all value and reference types serialized via WriteToStream.")]
        public void ReadFromStreamRoundTripsWriteToStream()
        {
            TestJsonMediaTypeFormatter formatter = new TestJsonMediaTypeFormatter();
            HttpContentHeaders contentHeaders = new StringContent(string.Empty).Headers;

            TestDataAssert.Execute(
                TestData.ValueAndRefTypeTestDataCollection,
                (type, obj) =>
                {
                    bool canSerialize = IsTypeSerializableWithJsonSerializer(type, obj) && HttpTestData.CanRoundTrip(type);
                    if (canSerialize)
                    {
                        object readObj = null;
                        StreamAssert.WriteAndRead(
                            (stream) => formatter.WriteToStream(type, obj, stream, contentHeaders, /*transportContext*/ null),
                            (stream) => readObj = formatter.ReadFromStream(type, stream, contentHeaders));
                        TestDataAssert.AreEqual(obj, readObj, "Failed to round trip object.");
                    }
                });
        }

        #endregion ReadFromStream()

        #region ReadFromStreamAsync()

        [TestMethod]
        [TestCategory("CIT"), Timeout(TimeoutConstant.DefaultTimeout), Owner("")]
        [Description("JsonMediaTypeFormatter.ReadFromStreamAsync returns all value and reference types serialized via WriteToStreamAsync.")]
        public void ReadFromStreamAsyncRoundTripsWriteToStreamAsync()
        {
            TestJsonMediaTypeFormatter formatter = new TestJsonMediaTypeFormatter();
            HttpContentHeaders contentHeaders = new StringContent(string.Empty).Headers;

            TestDataAssert.Execute(
                TestData.RepresentativeValueAndRefTypeTestDataCollection,
                (type, obj) =>
                {
                    bool canSerialize = IsTypeSerializableWithJsonSerializer(type, obj) && HttpTestData.CanRoundTrip(type);
                    if (canSerialize)
                    {
                        object readObj = null;
                        StreamAssert.WriteAndRead(
                            (stream) => TaskAssert.Succeeds(formatter.WriteToStreamAsync(type, obj, stream, contentHeaders, /*transportContext*/ null)),
                            (stream) => readObj = TaskAssert.SucceedsWithResult(formatter.ReadFromStreamAsync(type, stream, contentHeaders)));
                        TestDataAssert.AreEqual(obj, readObj, "Failed to round trip object.");
                    }
                });
        }

        #endregion ReadFromStreamAsync()

        #endregion Methods

        #region Test types

        public class TestJsonMediaTypeFormatter : JsonMediaTypeFormatter
        {
            public bool CanReadTypeProxy(Type type)
            {
                return this.CanReadType(type);
            }

            public bool CanWriteTypeProxy(Type type)
            {
                return this.CanWriteType(type);
            }
        }

        #endregion Test types

        #region Test helpers

        private static bool IsTypeSerializableWithJsonSerializer(Type type, object obj)
        {
            try
            {
                new DataContractJsonSerializer(type);
                if (obj != null && obj.GetType() != type)
                {
                    new DataContractJsonSerializer(obj.GetType());
                }
            }
            catch
            {
                return false;
            }

            return !HttpTestData.IsKnownUnserializable(type, obj, (t) => typeof(INotJsonSerializable).IsAssignableFrom(t));
        }

        #endregion Test helpers

    }
}
