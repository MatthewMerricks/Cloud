// <copyright>
//   Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>

namespace Microsoft.ApplicationServer.Http
{
    using System;
    using System.Collections.ObjectModel;
    using System.IO;
    using System.Linq;
    using System.Net.Http;
    using System.Net.Http.Headers;
    using System.Net.Http.Moles;
    using System.Threading.Tasks;
    using Microsoft.ApplicationServer.Common.Test;
    using Microsoft.ApplicationServer.Http.Moles;
    using Microsoft.ApplicationServer.Http.Test;
    using Microsoft.VisualStudio.TestTools.UnitTesting;

    [TestClass, UnitTestLevel(UnitTestLevel.InProgress)]
    public class MediaTypeFormatterTests : UnitTest<MediaTypeFormatter>
    {
        [TestCleanup, HostType("Moles")]
        public void TestCleanup()
        {
            // Ensure every test resets Moled object back to default
            MMediaTypeFormatter.BehaveAsCurrent();
        }

        #region Type

        [TestMethod]
        [TestCategory("CIT"), Timeout(TimeoutConstant.DefaultTimeout), Owner("")]
        [Description("MediaTypeFormatter is public, abstract, and unsealed.")]
        public void TypeIsCorrect()
        {
            Type t = this.TypeUnderTest;
            Assert.IsTrue(t.IsPublic, "MediaTypeFormatter should be public");
            Assert.IsTrue(t.IsAbstract, "MediaTypeFormatter should be abstract");
            Assert.IsFalse(t.IsSealed, "MediaTypeFormatter should not be sealed");
        }

        #endregion Type

        #region Constructors

        [TestMethod]
        [TestCategory("CIT"), Timeout(TimeoutConstant.DefaultTimeout), Owner("")]
        [Description("MediaTypeFormatter() constructor (via derived class) sets SupportedMediaTypes and MediaTypeMappings.")]
        public void Constructor()
        {
            SMediaTypeFormatter formatter = new SMediaTypeFormatter() { CallBase = true };
            Collection<MediaTypeHeaderValue> supportedMediaTypes = formatter.SupportedMediaTypes;
            Assert.IsNotNull(supportedMediaTypes, "SupportedMediaTypes was not initialized.");
            Assert.AreEqual(0, supportedMediaTypes.Count, "SupportedMediaTypes should be empty by default.");

            Collection<MediaTypeMapping> mappings = formatter.MediaTypeMappings;
            Assert.IsNotNull(mappings, "MediaTypeMappings was not initialized.");
            Assert.AreEqual(0, mappings.Count, "MediaTypeMappings should be empty by default.");
        }

        #endregion Constructors

        #region Properties

        #region SupportedMediaTypes

        [TestMethod]
        [TestCategory("CIT"), Timeout(TimeoutConstant.DefaultTimeout), Owner("")]
        [Description("SupportedMediaTypes is a mutable collection.")]
        public void SupportedMediaTypesIsMutable()
        {
            SMediaTypeFormatter formatter = new SMediaTypeFormatter() { CallBase = true };
            Collection<MediaTypeHeaderValue> supportedMediaTypes = formatter.SupportedMediaTypes;
            MediaTypeHeaderValue[] mediaTypes = HttpTestData.LegalMediaTypeHeaderValues.ToArray();
            foreach (MediaTypeHeaderValue mediaType in mediaTypes)
            {
                supportedMediaTypes.Add(mediaType);
            }

            CollectionAssert.AreEqual(mediaTypes, formatter.SupportedMediaTypes, "SupportedMediaTypes does not contain expected set of media types.");
        }

        #region SupportedMediaTypes.Add()

        [TestMethod]
        [TestCategory("CIT"), Timeout(TimeoutConstant.DefaultTimeout), Owner("")]
        [Description("SupportedMediaTypes.Add throws with a null media type.")]
        public void SupportedMediaTypesAddThrowsWithNullMediaType()
        {
            SMediaTypeFormatter formatter = new SMediaTypeFormatter() { CallBase = true };
            Collection<MediaTypeHeaderValue> supportedMediaTypes = formatter.SupportedMediaTypes;

            ExceptionAssert.ThrowsArgumentNull(
                "item",
                () => supportedMediaTypes.Add(null));
        }

        [TestMethod]
        [TestCategory("CIT"), Timeout(TimeoutConstant.DefaultTimeout), Owner("")]
        [Description("SupportedMediaTypes.Add throws with a media range.")]
        public void SupportedMediaTypesAddThrowsWithMediaRange()
        {
            SMediaTypeFormatter formatter = new SMediaTypeFormatter() { CallBase = true };
            Collection<MediaTypeHeaderValue> supportedMediaTypes = formatter.SupportedMediaTypes;
            MediaTypeHeaderValue[] mediaRanges = HttpTestData.LegalMediaRangeValues.ToArray();
            foreach (MediaTypeHeaderValue mediaType in mediaRanges)
            {
                ExceptionAssert.ThrowsArgument(
                    "item",
                    SR.CannotUseMediaRangeForSupportedMediaType(typeof(MediaTypeHeaderValue).Name, mediaType.MediaType),
                    () => supportedMediaTypes.Add(mediaType));
            }
        }

        #endregion SupportedMediaTypes.Add()

        #region SupportedMediaTypes.Insert()

        [TestMethod]
        [TestCategory("CIT"), Timeout(TimeoutConstant.DefaultTimeout), Owner("")]
        [Description("SupportedMediaTypes.Insert throws with a null media type.")]
        public void SupportedMediaTypesInsertThrowsWithNullMediaType()
        {
            SMediaTypeFormatter formatter = new SMediaTypeFormatter() { CallBase = true };
            Collection<MediaTypeHeaderValue> supportedMediaTypes = formatter.SupportedMediaTypes;

            ExceptionAssert.ThrowsArgumentNull(
                "item",
                () => supportedMediaTypes.Insert(0, null));
        }

        [TestMethod]
        [TestCategory("CIT"), Timeout(TimeoutConstant.DefaultTimeout), Owner("")]
        [Description("SupportedMediaTypes.Insert throws with a media range.")]
        public void SupportedMediaTypesInsertThrowsWithMediaRange()
        {
            SMediaTypeFormatter formatter = new SMediaTypeFormatter() { CallBase = true };
            Collection<MediaTypeHeaderValue> supportedMediaTypes = formatter.SupportedMediaTypes;
            MediaTypeHeaderValue[] mediaRanges = HttpTestData.LegalMediaRangeValues.ToArray();
            foreach (MediaTypeHeaderValue mediaType in mediaRanges)
            {
                ExceptionAssert.ThrowsArgument(
                    "item",
                    SR.CannotUseMediaRangeForSupportedMediaType(typeof(MediaTypeHeaderValue).Name, mediaType.MediaType),
                    () => supportedMediaTypes.Insert(0, mediaType));
            }
        }

        #endregion SupportedMediaTypes.Add()

        #endregion SupportedMediaTypes

        #region MediaTypeMappings

        [TestMethod]
        [TestCategory("CIT"), Timeout(TimeoutConstant.DefaultTimeout), Owner("")]
        [Description("MediaTypeMappings is a mutable collection.")]
        public void MediaTypeMappingsIsMutable()
        {
            SMediaTypeFormatter formatter = new SMediaTypeFormatter() { CallBase = true };
            Collection<MediaTypeMapping> mappings = formatter.MediaTypeMappings;
            MediaTypeMapping[] standardMappings = HttpTestData.StandardMediaTypeMappings.ToArray();
            foreach (MediaTypeMapping mapping in standardMappings)
            {
                mappings.Add(mapping);
            }

            CollectionAssert.AreEqual(standardMappings, formatter.MediaTypeMappings, "MediaTypeMappings does not contain expected set of MediaTypeMapping elements.");
        }

        #endregion MediaTypeMappings

        #endregion Properties

        #region Methods

        #region CanReadAs(Type, HttpContent)

        [TestMethod]
        [TestCategory("CIT"), Timeout(TimeoutConstant.DefaultTimeout), Owner("")]
        [Description("CanReadAs(Type, HttpContent) returns true for all standard media types.")]
        public void CanReadAsReturnsTrue()
        {
            SMediaTypeFormatter formatter = new SMediaTypeFormatter() { CallBase = true };
            string[] legalMediaTypeStrings = HttpTestData.LegalMediaTypeStrings.ToArray();
            foreach (string mediaType in legalMediaTypeStrings)
            {
                formatter.SupportedMediaTypes.Add(new MediaTypeHeaderValue(mediaType));
            }

            TestDataAssert.Execute(
                TestData.RepresentativeValueAndRefTypeTestDataCollection,
                (type, obj) =>
                {
                    foreach (string mediaType in legalMediaTypeStrings)
                    {
                        SStringContent content = new SStringContent("data") { CallBase = true };
                        content.Headers.ContentType = new MediaTypeHeaderValue(mediaType);
                        Assert.IsTrue(formatter.CanReadAs(type, content), string.Format("CanReadAs should have returned true for '{0}'.", type));
                    }
                });
        }

        [TestMethod]
        [TestCategory("CIT"), Timeout(TimeoutConstant.DefaultTimeout), Owner("")]
        [Description("CanReadAs(Type, HttpContent) throws with null type.")]
        public void CanReadAsThrowsWithNullType()
        {
            SMediaTypeFormatter formatter = new SMediaTypeFormatter() { CallBase = true };
            SStringContent content = new SStringContent("data") { CallBase = true };
            ExceptionAssert.ThrowsArgumentNull("type", () => formatter.CanReadAs(null, content));
        }

        [TestMethod]
        [TestCategory("CIT"), Timeout(TimeoutConstant.DefaultTimeout), Owner("")]
        [Description("CanReadAs(Type, HttpContent) throws with null content.")]
        public void CanReadAsThrowsWithNullContent()
        {
            SMediaTypeFormatter formatter = new SMediaTypeFormatter() { CallBase = true };
            ExceptionAssert.ThrowsArgumentNull("content", () => formatter.CanReadAs(typeof(int), (HttpContent)null));
        }

        #endregion CanReadAs(Type, HttpContent)

        #region CanReadAs(Type, HttpRequestMessage, out MediaTypeHeaderValue)

        [TestMethod]
        [TestCategory("CIT"), Timeout(TimeoutConstant.DefaultTimeout), Owner("")]
        [Description("CanReadAs(ObjectContent, HttpRequestMessage, out MediaTypeHeaderValue) returns true for all standard media types.")]
        public void CanReadAs1ReturnsTrue()
        {
            SMediaTypeFormatter formatter = new SMediaTypeFormatter() { CallBase = true };
            string[] legalMediaTypeStrings = HttpTestData.LegalMediaTypeStrings.ToArray();
            foreach (string mediaType in legalMediaTypeStrings)
            {
                formatter.SupportedMediaTypes.Add(new MediaTypeHeaderValue(mediaType));
            }

            TestDataAssert.Execute(
                TestData.RepresentativeValueAndRefTypeTestDataCollection,
                (type, obj) =>
                {
                    foreach (string mediaType in legalMediaTypeStrings)
                    {
                        SObjectContent objectContent = new SObjectContent(type, obj);
                        objectContent.Headers.ContentType = formatter.SupportedMediaTypes[0];
                        Assert.IsTrue(formatter.CanReadAs(type, new HttpRequestMessage() { Content = objectContent }), string.Format("CanReadAs should have returned true for '{0}'.", type));
                    }
                });
        }

        [TestMethod]
        [TestCategory("CIT"), Timeout(TimeoutConstant.DefaultTimeout), Owner("")]
        [Description("CanReadAs(Type, HttpRequestMessage) throws with null ObjectContent.")]
        public void CanReadAs1ThrowsWithNullContent()
        {
            SMediaTypeFormatter formatter = new SMediaTypeFormatter() { CallBase = true };
            ExceptionAssert.ThrowsArgumentNull("type", () => formatter.CanReadAs((Type)null, new HttpRequestMessage()));
        }

        #endregion CanReadAs(Type, HttpRequestMessage, out MediaTypeHeaderValue)

        #region CanReadAs(Type, HttpResponseMessage, out MediaTypeHeaderValue)

        [TestMethod]
        [TestCategory("CIT"), Timeout(TimeoutConstant.DefaultTimeout), Owner("")]
        [Description("CanReadAs(Type, HttpResponseMessage) returns true for all standard media types.")]
        public void CanReadAs2ReturnsTrue()
        {
            SMediaTypeFormatter formatter = new SMediaTypeFormatter() { CallBase = true };
            string[] legalMediaTypeStrings = HttpTestData.LegalMediaTypeStrings.ToArray();
            foreach (string mediaType in legalMediaTypeStrings)
            {
                formatter.SupportedMediaTypes.Add(new MediaTypeHeaderValue(mediaType));
            }

            TestDataAssert.Execute(
                TestData.RepresentativeValueAndRefTypeTestDataCollection,
                (type, obj) =>
                {
                    foreach (string mediaType in legalMediaTypeStrings)
                    {
                        SObjectContent objectContent = new SObjectContent(type, obj);
                        objectContent.Headers.ContentType = formatter.SupportedMediaTypes[0];
                        Assert.IsTrue(formatter.CanReadAs(type, new HttpResponseMessage() { Content = objectContent }), string.Format("CanReadAs should have returned true for '{0}'.", type));
                    }
                });
        }

        [TestMethod]
        [TestCategory("CIT"), Timeout(TimeoutConstant.DefaultTimeout), Owner("")]
        [Description("CanReadAs(Type, HttpResponseMessage) throws with null Type.")]
        public void CanReadAs2ThrowsWithNullContent()
        {
            SMediaTypeFormatter formatter = new SMediaTypeFormatter() { CallBase = true };
            ExceptionAssert.ThrowsArgumentNull("type", () => formatter.CanReadAs((Type)null, new HttpResponseMessage()));
        }

        #endregion CanReadAs(Type, HttpResponseMessage, out MediaTypeHeaderValue)

        #region CanWriteAsAs(Type, HttpContent, out MediaTypeHeaderValue)

        [TestMethod]
        [TestCategory("CIT"), Timeout(TimeoutConstant.DefaultTimeout), Owner("")]
        [Description("CanWriteAs(Type, HttpContent, out MediaTypeHeaderValue) returns true always for supported media types.")]
        public void CanWriteAsReturnsTrue()
        {
            SMediaTypeFormatter formatter = new SMediaTypeFormatter() { CallBase = true };
            foreach (string mediaType in HttpTestData.LegalMediaTypeStrings)
            {
                formatter.SupportedMediaTypes.Add(new MediaTypeHeaderValue(mediaType));
            }

            TestDataAssert.Execute(
                TestData.RepresentativeValueAndRefTypeTestDataCollection,
                (type, obj) =>
                {
                    MediaTypeHeaderValue matchedMediaType = null;
                    SObjectContent objectContent = new SObjectContent(type, obj) { CallBase = true };
                    objectContent.Headers.ContentType = formatter.SupportedMediaTypes[0];
                    Assert.IsTrue(formatter.CanWriteAs(type, objectContent, out matchedMediaType), string.Format("CanWriteAs should have returned true for '{0}'.", type));
                });
        }

        [TestMethod]
        [TestCategory("CIT"), Timeout(TimeoutConstant.DefaultTimeout), Owner("")]
        [Description("CanWriteAs(Type, HttpContent, out MediaTypeHeaderValue) throws with null content.")]
        public void CanWriteAsThrowsWithNullContent()
        {
            SMediaTypeFormatter formatter = new SMediaTypeFormatter() { CallBase = true };
            MediaTypeHeaderValue mediaType = null;
            ExceptionAssert.ThrowsArgumentNull("content", () => formatter.CanWriteAs(typeof(int), (HttpContent)null, out mediaType));
        }

        #endregion CanWriteAs(Type, HttpContent, out MediaTypeHeaderValue)

        #region CanWriteAsAs(Type, HttpRequestMessage, out MediaTypeHeaderValue)

        [TestMethod]
        [TestCategory("CIT"), Timeout(TimeoutConstant.DefaultTimeout), Owner("")]
        [Description("CanWriteAs(Type, HttpRequestMessage, out MediaTypeHeaderValue) returns true always for supported media types.")]
        public void CanWriteAs1ReturnsTrue()
        {
            SMediaTypeFormatter formatter = new SMediaTypeFormatter() { CallBase = true };
            foreach (string mediaType in HttpTestData.LegalMediaTypeStrings)
            {
                formatter.SupportedMediaTypes.Add(new MediaTypeHeaderValue(mediaType));
            }

            TestDataAssert.Execute(
                TestData.RepresentativeValueAndRefTypeTestDataCollection,
                (type, obj) =>
                {
                    MediaTypeHeaderValue matchedMediaType = null;
                    SObjectContent objectContent = new SObjectContent(type, obj) { CallBase = true };
                    objectContent.Headers.ContentType = formatter.SupportedMediaTypes[0];
                    Assert.IsTrue(formatter.CanWriteAs(type, new HttpRequestMessage() { Content = objectContent }, out matchedMediaType), string.Format("CanWriteAs should have returned true for '{0}'.", type));
                });
        }

        [TestMethod]
        [TestCategory("CIT"), Timeout(TimeoutConstant.DefaultTimeout), Owner("")]
        [Description("CanWriteAs(Type, HttpRequestMessage, out MediaTypeHeaderValue) throws with null type.")]
        public void CanWriteAs1ThrowsWithNullType()
        {
            SMediaTypeFormatter formatter = new SMediaTypeFormatter() { CallBase = true };
            MediaTypeHeaderValue mediaType = null;
            ExceptionAssert.ThrowsArgumentNull("type", () => formatter.CanWriteAs(null, new HttpRequestMessage(), out mediaType));
        }

        #endregion CanWriteAs(Type, HttpRequestMessage, out MediaTypeHeaderValue)

        #region CanWriteAsAs(Type, HttpResponseMessage, out MediaTypeHeaderValue)

        [TestMethod]
        [TestCategory("CIT"), Timeout(TimeoutConstant.DefaultTimeout), Owner("")]
        [Description("CanWriteAs(Type, HttpResponseMessage, out MediaTypeHeaderValue) returns true always for supported media types.")]
        public void CanWriteAs2ReturnsTrue()
        {
            SMediaTypeFormatter formatter = new SMediaTypeFormatter() { CallBase = true };
            foreach (string mediaType in HttpTestData.LegalMediaTypeStrings)
            {
                formatter.SupportedMediaTypes.Add(new MediaTypeHeaderValue(mediaType));
            }

            TestDataAssert.Execute(
                TestData.RepresentativeValueAndRefTypeTestDataCollection,
                (type, obj) =>
                {
                    MediaTypeHeaderValue matchedMediaType = null;
                    SObjectContent objectContent = new SObjectContent(type, obj) { CallBase = true };
                    objectContent.Headers.ContentType = formatter.SupportedMediaTypes[0];
                    Assert.IsTrue(formatter.CanWriteAs(type, new HttpResponseMessage() { Content = objectContent }, out matchedMediaType), string.Format("CanWriteAs should have returned true for '{0}'.", type));
                });
        }

        [TestMethod]
        [TestCategory("CIT"), Timeout(TimeoutConstant.DefaultTimeout), Owner("")]
        [Description("CanWriteAs(Type, HttpResponseMessage, out MediaTypeHeaderValue) throws with null type.")]
        public void CanWriteAs2ThrowsWithNullContent()
        {
            SMediaTypeFormatter formatter = new SMediaTypeFormatter() { CallBase = true };
            MediaTypeHeaderValue mediaType = null;
            ExceptionAssert.ThrowsArgumentNull("type", () => formatter.CanWriteAs(null, new HttpResponseMessage(), out mediaType));
        }

        #endregion CanWriteAs(Type, HttpResponseMessage, out MediaTypeHeaderValue)

        #region CanReadType()

        [Ignore]        //// TODO: pending answer from Moles
        [TestMethod]
        [TestCategory("CIT"), Timeout(TimeoutConstant.DefaultTimeout), Owner("")]
        [Description("CanReadType() base implementation returns true always.")]
        public void CanReadTypeReturnsTrue()
        {
            SMediaTypeFormatter formatter = new SMediaTypeFormatter() { CallBase = true };
            string[] legalMediaTypeStrings = HttpTestData.LegalMediaTypeStrings.ToArray();
            foreach (string mediaType in legalMediaTypeStrings)
            {
                formatter.SupportedMediaTypes.Add(new MediaTypeHeaderValue(mediaType));
            }

            TestDataAssert.Execute(
                TestData.RepresentativeValueAndRefTypeTestDataCollection,
                (type, obj) =>
                {
                    foreach (string mediaType in legalMediaTypeStrings)
                    {
                        SStringContent content = new SStringContent("data") { CallBase = true };
                        content.Headers.ContentType = new MediaTypeHeaderValue(mediaType);
                        Assert.IsTrue(formatter.CanReadAs(type, content), string.Format("CanReadType should have returned true for '{0}'.", type));
                    }
                });
        }

        #endregion CanReadType()

        #region CanWriteType()

        [Ignore]            //// TODO: pending answer from Moles
        [TestMethod]
        [TestCategory("CIT"), Timeout(TimeoutConstant.DefaultTimeout), Owner("")]
        [Description("CanWriteType() base implementation returns true always.")]
        public void CanWriteTypeReturnsTrue()
        {
            SMediaTypeFormatter formatter = new SMediaTypeFormatter() { CallBase = true };
            foreach (string mediaType in HttpTestData.LegalMediaTypeStrings)
            {
                formatter.SupportedMediaTypes.Add(new MediaTypeHeaderValue(mediaType));
            }

            TestDataAssert.Execute(
                TestData.RepresentativeValueAndRefTypeTestDataCollection,
                (type, obj) =>
                {
                    MediaTypeHeaderValue matchedMediaType = null;
                    SObjectContent objectContent = new SObjectContent(type, obj) { CallBase = true };
                    Assert.IsTrue(formatter.CanWriteAs(type, objectContent, out matchedMediaType), string.Format("CanWriteType should have returned true for '{0}'.", type));
                });
        }

        #endregion CanWriteType()

        #region ReadFromStream()

        [TestMethod]
        [TestCategory("CIT"), Timeout(TimeoutConstant.DefaultTimeout), Owner("")]
        [Description("ReadFromStream() calls protected OnReadFromStream().")]
        public void ReadFromStreamCallsOnReadFromStream()
        {
            Type calledType = null;
            Stream calledStream = null;
            HttpContentHeaders calledHeaders = null;

            SMediaTypeFormatter formatter = new SMediaTypeFormatter() { CallBase = true };
            formatter.OnReadFromStreamTypeStreamHttpContentHeaders = (type, stream, headers) =>
                {
                    calledType = type;
                    calledStream = stream;
                    calledHeaders = headers;
                    return null;
                };
            SHttpContent content = new SHttpContent();
            HttpContentHeaders contentHeaders = content.Headers;

            StreamAssert.WriteAndRead(
                (stream) => { },
                (stream) => formatter.ReadFromStream(typeof(int), stream, contentHeaders));

            Assert.AreEqual(typeof(int), calledType, "OnReadFromStream was not called or did not pass Type.");
            Assert.IsNotNull(calledStream, "OnReadFromStream was not called or did not pass Type.");
            Assert.AreSame(contentHeaders, calledHeaders, "OnReadFromStream was not called or did not pass ContentHeaders.");
        }

        #endregion ReadFromStream()

        #region ReadFromStreamAsync()

        [TestMethod]
        [TestCategory("CIT"), Timeout(TimeoutConstant.DefaultTimeout), Owner("")]
        [Description("ReadFromStreamAsync() calls protected OnReadFromStreamAsync().")]
        public void ReadFromStreamAsyncCallsOnReadFromStreamAsync()
        {
            Type calledType = null;
            Stream calledStream = null;
            HttpContentHeaders calledHeaders = null;

            SMediaTypeFormatter formatter = new SMediaTypeFormatter() { CallBase = true };
            formatter.OnReadFromStreamAsyncTypeStreamHttpContentHeaders = (type, stream, headers) =>
            {
                calledType = type;
                calledStream = stream;
                calledHeaders = headers;
                return null;
            };
            SHttpContent content = new SHttpContent();
            HttpContentHeaders contentHeaders = content.Headers;

            StreamAssert.WriteAndRead(
                (stream) => { },
                (stream) => formatter.ReadFromStreamAsync(typeof(int), stream, contentHeaders));

            Assert.AreEqual(typeof(int), calledType, "OnReadFromStreamAsync was not called or did not pass Type.");
            Assert.IsNotNull(calledStream, "OnReadFromStreamAsync was not called or did not pass Type.");
            Assert.AreSame(contentHeaders, calledHeaders, "OnReadFromStreamAsync was not called or did not pass ContentHeaders.");
        }

        [TestMethod]
        [TestCategory("CIT"), Timeout(TimeoutConstant.DefaultTimeout), Owner("")]
        [Description("ReadFromStreamAsync() calls protected OnReadFromStream and wraps a Task around it().")]
        public void ReadFromStreamAsyncCallsOnReadFromStreamInTask()
        {
            Type calledType = null;
            Stream calledStream = null;
            HttpContentHeaders calledHeaders = null;

            SMediaTypeFormatter formatter = new SMediaTypeFormatter() { CallBase = true };
            formatter.OnReadFromStreamTypeStreamHttpContentHeaders = (type, stream, headers) =>
            {
                calledType = type;
                calledStream = stream;
                calledHeaders = headers;
                return 5;
            };

            SHttpContent content = new SHttpContent();
            HttpContentHeaders contentHeaders = content.Headers;

            Task<object> createdTask =
                StreamAssert.WriteAndReadResult<Task<object>>(
                    (stream) => { },
                    (stream) => formatter.ReadFromStreamAsync(typeof(int), stream, contentHeaders));

            object readObject = TaskAssert.SucceedsWithResult(createdTask);
            Assert.AreEqual(5, readObject, "ReadFromStreamAsync should have returned this value from stub.");
            Assert.AreEqual(typeof(int), calledType, "OnReadFromStreamAsync was not called or did not pass Type.");
            Assert.IsNotNull(calledStream, "OnReadFromStreamAsync was not called or did not pass Type.");
            Assert.AreSame(contentHeaders, calledHeaders, "OnReadFromStreamAsync was not called or did not pass ContentHeaders.");
        }

        #endregion ReadFromStreamAsync()

        #region WriteToStream()

        [TestMethod]
        [TestCategory("CIT"), Timeout(TimeoutConstant.DefaultTimeout), Owner("")]
        [Description("WriteToStream() calls protected OnWriteToStream().")]
        public void WriteToStreamCallsOnWriteToStream()
        {
            Type calledType = null;
            object calledObj = null;
            Stream calledStream = null;
            HttpContentHeaders calledHeaders = null;

            SMediaTypeFormatter formatter = new SMediaTypeFormatter() { CallBase = true };
            formatter.OnWriteToStreamTypeObjectStreamHttpContentHeadersTransportContext = (type, obj, stream, headers, context) =>
            {
                calledType = type;
                calledObj = obj;
                calledStream = stream;
                calledHeaders = headers;
            };
            SHttpContent content = new SHttpContent();
            HttpContentHeaders contentHeaders = content.Headers;

            StreamAssert.WriteAndRead(
                (stream) => formatter.WriteToStream(typeof(int), 5,  stream, contentHeaders, /*transportContext*/ null),
                (stream) => {});

            Assert.AreEqual(typeof(int), calledType, "OnWriteToStream was not called or did not pass Type.");
            Assert.AreEqual(5, calledObj, "OnWriteToStream was not called or did not pass the object value.");
            Assert.IsNotNull(calledStream, "OnWriteToStream was not called or did not pass Type.");
            Assert.AreSame(contentHeaders, calledHeaders, "OnWriteToStream was not called or did not pass ContentHeaders.");
        }

        #endregion WriteToStream()

        #region WriteToStreamAsync()

        [TestMethod]
        [TestCategory("CIT"), Timeout(TimeoutConstant.DefaultTimeout), Owner("")]
        [Description("WriteToStreamAsync() calls protected OnWriteToStreamAsync().")]
        public void WriteToStreamAsyncCallsOnWriteToStreamAsync()
        {
            Type calledType = null;
            object calledObj = null;
            Stream calledStream = null;
            HttpContentHeaders calledHeaders = null;
            Task calledTask = null;
            Task createdTask = new Task(() => { });

            SMediaTypeFormatter formatter = new SMediaTypeFormatter() { CallBase = true };
            formatter.OnWriteToStreamAsyncTypeObjectStreamHttpContentHeadersTransportContext = (type, obj, stream, headers, context) =>
            {
                calledType = type;
                calledObj = obj;
                calledStream = stream;
                calledHeaders = headers;
                return createdTask;
            };

            SHttpContent content = new SHttpContent();
            HttpContentHeaders contentHeaders = content.Headers;

            StreamAssert.WriteAndRead(
                (stream) => calledTask = formatter.WriteToStreamAsync(typeof(int), 5, stream, contentHeaders, /*transportContext*/ null),
                (stream) => { });

            Assert.AreEqual(typeof(int), calledType, "OnWriteToStreamAsync was not called or did not pass Type.");
            Assert.AreEqual(5, calledObj, "OnWriteToStreamAsync was not called or did not pass the object value.");
            Assert.IsNotNull(calledStream, "OnWriteToStreamAsync was not called or did not pass Type.");
            Assert.AreSame(contentHeaders, calledHeaders, "OnWriteToStreamAsync was not called or did not pass ContentHeaders.");
            Assert.AreSame(createdTask, calledTask, "OnWriteToStreamAsync was not called or did not return the Task result.");
        }


        [TestMethod]
        [TestCategory("CIT"), Timeout(TimeoutConstant.DefaultTimeout), Owner("")]
        [Description("WriteToStreamAsync() calls protected OnWriteToStream() and wraps a Task around it.")]
        public void WriteToStreamAsyncCallsOnWriteToStreamInTask()
        {
            Type calledType = null;
            object calledObj = null;
            Stream calledStream = null;
            HttpContentHeaders calledHeaders = null;
            Task createdTask = null;

            SMediaTypeFormatter formatter = new SMediaTypeFormatter() { CallBase = true };
            formatter.OnWriteToStreamTypeObjectStreamHttpContentHeadersTransportContext = (type, obj, stream, headers, context) =>
            {
                calledType = type;
                calledObj = obj;
                calledStream = stream;
                calledHeaders = headers;
            };

            SHttpContent content = new SHttpContent();
            HttpContentHeaders contentHeaders = content.Headers;

            StreamAssert.WriteAndRead(
                (stream) => createdTask = formatter.WriteToStreamAsync(typeof(int), 5, stream, contentHeaders, /*transportContext*/ null),
                (stream) => { });

            TaskAssert.Succeeds(createdTask);
            Assert.AreEqual(typeof(int), calledType, "OnWriteToStream was not called or did not pass Type.");
            Assert.AreEqual(5, calledObj, "OnWriteToStream was not called or did not pass the object value.");
            Assert.IsNotNull(calledStream, "OnWriteToStream was not called or did not pass Type.");
            Assert.AreSame(contentHeaders, calledHeaders, "OnWriteToStream was not called or did not pass ContentHeaders.");
        }

        #endregion WriteToStreamAsync()

        #endregion Methods

    }
}
