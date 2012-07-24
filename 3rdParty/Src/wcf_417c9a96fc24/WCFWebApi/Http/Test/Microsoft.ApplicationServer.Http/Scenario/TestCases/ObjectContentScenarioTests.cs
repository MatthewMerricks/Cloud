// <copyright>
// Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>

namespace Microsoft.ApplicationServer.Http.CIT.Scenario
{
    using System;
    using System.Collections.Generic;
    using System.Net;
    using System.Net.Http;
    using System.Net.Http.Headers;
    using System.Runtime.Serialization;
    using System.Runtime.Serialization.Json;
    using System.Threading;
    using System.Xml.Serialization;
    using Microsoft.ApplicationServer.Common.Test;
    using Microsoft.ApplicationServer.Common.Test.Services;
    using Microsoft.ApplicationServer.Http;
    using Microsoft.VisualStudio.TestTools.UnitTesting;

    [TestClass]
    public class ObjectContentScenarioTests
    {
        private static MediaTypeWithQualityHeaderValue XmlMediaType = new MediaTypeWithQualityHeaderValue("application/xml");
        private static MediaTypeWithQualityHeaderValue JsonMediaType = new MediaTypeWithQualityHeaderValue("application/json");

        #region User Scenarios

        #region Http Get as strongly-typed object

        #region Default Xml serializer

        [TestMethod]
        [TestCategory("CIT"), Timeout(TimeoutConstant.ExtendedTimeout), Owner("dravva")]
        [Description("Demonstrates how to treat the response from an Http GET as a strongly typed object using the default Xml serializer.")]
        public void GetPocoXml()
        {
            GenericWebService service = GenericWebService.GetServiceInstance(typeof(MyPocoType), typeof(MyPocoType));
            service.OnGetReturnInstance = () => new MyPocoType() { Id = 1, Name = "Mary" };

            HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Get, new Uri(GenericWebService.UriTemplate, UriKind.Relative));
            HttpServiceHostAssert.Execute(
                service,
                request,
                (response) =>
                {
                    Assert.AreEqual(HttpStatusCode.OK, response.StatusCode, "Response status code should have been a 200.");
                    MyPocoType pocoResult = response.Content.ReadAs<MyPocoType>();
                    Assert.AreEqual("Mary", pocoResult.Name, "Failed to read MyPocoType using default Xml serializer.");
                });
        }

        #endregion Default Xml serializer

        #region DataContract Xml serializer

        [TestMethod]
        [TestCategory("CIT"), Timeout(TimeoutConstant.ExtendedTimeout), Owner("dravva")]
        [Description("Demonstrates how to treat the response from an Http GET as a strongly typed object using the DataContract Xml serializer.")]
        public void GetPocoXmlDataContract()
        {
            GenericWebService service = GenericWebService.GetServiceInstance(typeof(MyPocoType), typeof(MyPocoType), useDataContract:true);
            service.OnGetReturnInstance = () => new MyPocoType() { Id = 1, Name = "Mary" };

            XmlMediaTypeFormatter formatter = new XmlMediaTypeFormatter();
            formatter.SetSerializer(typeof(MyPocoType), new DataContractSerializer(typeof(MyPocoType)));

            HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Get, new Uri(GenericWebService.UriTemplate, UriKind.Relative));
            HttpServiceHostAssert.Execute(
                service,
                request,
                (response) =>
                {
                    Assert.AreEqual(HttpStatusCode.OK, response.StatusCode, "Response status code should have been a 200.");
                    MyPocoType pocoResult = response.Content.ReadAs<MyPocoType>(new MediaTypeFormatter[] { formatter });
                    Assert.AreEqual("Mary", pocoResult.Name, "Failed to read MyPocoType using DataContract Xml serializer.");
                });
        }

        #endregion DataContract Xml serializer

        #region DataContract Json serializer

        [TestMethod]
        [TestCategory("CIT"), Timeout(TimeoutConstant.ExtendedTimeout), Owner("dravva")]
        [Description("Demonstrates how to treat the response from an Http GET as a strongly typed object using the default Json serializer.")]
        public void GetPocoJson()
        {
            GenericWebService service = GenericWebService.GetServiceInstance(typeof(MyPocoType), typeof(MyPocoType));
            service.OnGetReturnInstance = () => new MyPocoType() { Id = 1, Name = "Mary" };

            HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Get, new Uri(GenericWebService.UriTemplate, UriKind.Relative));
            request.Headers.Accept.Add(JsonMediaType);

            HttpServiceHostAssert.Execute(
                service,
                request,
                (response) =>
                {
                    Assert.AreEqual(HttpStatusCode.OK, response.StatusCode, "Response status code should have been a 200.");
                    MyPocoType pocoResult = response.Content.ReadAs<MyPocoType>();
                    Assert.AreEqual("Mary", pocoResult.Name, "Failed to read MyPocoType using Json serializer.");
                });
        }

        #endregion DataContract Json serializer

        #endregion Http Get as strongly-typed object

        #region Http POST and read strongly-typed object

        #region Default Xml serializer

        [Ignore]
        [TestMethod]
        [TestCategory("CIT"), Timeout(TimeoutConstant.ExtendedTimeout), Owner("dravva")]
        [Description("Demonstrates how to POST and then read a strongly typed object using the default Xml serializer.")]
        public void PostPocoXml()
        {
            GenericWebService service = GenericWebService.GetServiceInstance(typeof(MyPocoType), typeof(MyPocoType));
            MyPocoType pocoPosted = new MyPocoType() { Id = 1, Name = "Mary" };

            HttpRequestMessage<MyPocoType> request = new HttpRequestMessage<MyPocoType>(
                                                            pocoPosted,
                                                            HttpMethod.Post,
                                                            new Uri(GenericWebService.UriTemplate, UriKind.Relative),
                                                            new MediaTypeFormatter[0]);
            request.Content.Headers.ContentType = new MediaTypeHeaderValue("application/xml");
            HttpServiceHostAssert.Execute(
                service,
                request,
                (response) =>
                {
                    Assert.AreEqual(HttpStatusCode.OK, response.StatusCode, "Response status code should have been a 200.");
                    MyPocoType pocoResult = response.Content.ReadAs<MyPocoType>();
                    Assert.AreEqual(pocoPosted.Name, pocoResult.Name, "Failed to POST then read MyPocoType using default Xml serializer.");
                });
        }

        #endregion Default Xml serializer

        #region DataContract Xml serializer

        [TestMethod]
        [TestCategory("CIT"), Timeout(TimeoutConstant.ExtendedTimeout), Owner("dravva")]
        [Description("Demonstrates how to POST and then read a strongly typed object using the DataContract Xml serializer.")]
        public void PostPocoXmlDataContract()
        {
            GenericWebService service = GenericWebService.GetServiceInstance(typeof(MyPocoType), typeof(MyPocoType), useDataContract:true);
            XmlMediaTypeFormatter formatter = new XmlMediaTypeFormatter();
            formatter.SetSerializer(typeof(MyPocoType), new DataContractSerializer(typeof(MyPocoType)));
            IEnumerable<MediaTypeFormatter> formatters = new MediaTypeFormatter[] { formatter };

            MyPocoType pocoPosted = new MyPocoType() { Id = 1, Name = "Mary" };

            HttpRequestMessage<MyPocoType> request = new HttpRequestMessage<MyPocoType>(
                                                            pocoPosted,
                                                            HttpMethod.Post,
                                                            new Uri(GenericWebService.UriTemplate, UriKind.Relative),
                                                            formatters);
            request.Content.Headers.ContentType = new MediaTypeHeaderValue("application/xml");
            HttpServiceHostAssert.Execute(
                service,
                request,
                (response) =>
                {
                    Assert.AreEqual(HttpStatusCode.OK, response.StatusCode, "Response status code should have been a 200.");
                    MyPocoType pocoResult = response.Content.ReadAs<MyPocoType>(formatters);
                    Assert.AreEqual(pocoPosted.Name, pocoResult.Name, "Failed to POST then read MyPocoType using DataContract Xml serializer.");
                });
        }

        #endregion DataContract Xml serializer

        #region DataContract Json serializer

        [Ignore]
        [TestMethod]
        [TestCategory("CIT"), Timeout(TimeoutConstant.ExtendedTimeout), Owner("dravva")]
        [Description("Demonstrates how to POST and then read a strongly typed object using the default Json serializer.")]
        public void PostPocoJson()
        {
            GenericWebService service = GenericWebService.GetServiceInstance(typeof(MyPocoType), typeof(MyPocoType));

            MyPocoType pocoPosted = new MyPocoType() { Id = 1, Name = "Mary" };

            HttpRequestMessage<MyPocoType> request = new HttpRequestMessage<MyPocoType>(
                                                            pocoPosted,
                                                            HttpMethod.Post,
                                                            new Uri(GenericWebService.UriTemplate, UriKind.Relative),
                                                            new MediaTypeFormatter[0]);

            request.Content.Headers.ContentType = JsonMediaType;
            request.Headers.Accept.Add(JsonMediaType);

            HttpServiceHostAssert.Execute(
                service,
                request,
                (response) =>
                {
                    Assert.AreEqual(HttpStatusCode.OK, response.StatusCode, "Response status code should have been a 200.");
                    MyPocoType pocoResult = response.Content.ReadAs<MyPocoType>();
                    Assert.AreEqual(pocoPosted.Name, pocoResult.Name, "Failed to POST then read MyPocoType using Json serializer.");
                });
        }

        #endregion DataContract Json serializer

        #endregion Http POST and read strongly-typed object

        #endregion User Scenarios

        #region Acceptance Scenarios

        #region ReadAs() from Http GET

        #region ReadAs() from Http GET using DataContractSerializer with Xml

        [TestMethod]
        [TestCategory("CIT"), Timeout(TimeoutConstant.ExtendedTimeout), Owner("dravva")]
        [Description("All value and reference types (including derived types) can be read using ReadAs() from Http GET using DataContractSerializer for XML.")]
        public void GetReadAsXmlDataContract()
        {
            IEnumerable<TestData> testData = TestData.RepresentativeValueAndRefTypeTestDataCollection;
            TestDataVariations variations = TestDataVariations.AllNonInterfaces;
            XmlMediaTypeFormatter formatter = new XmlMediaTypeFormatter();
            MediaTypeFormatter[] formatters = new MediaTypeFormatter[] { formatter };

            TestDataAssert.Execute(
                testData,
                variations,
                "ObjectContent serializing using the Xml DataContractSerializer failed.",
                (type, obj) =>
                {
                    GenericWebService service = GenericWebService.GetServiceInstance(type, obj.GetType(), useDataContract:true);
                    service.OnGetReturnInstance = () => obj;

                    HttpServiceHostAssert.Execute(
                        service,
                        GenericWebService.GetRequest(),
                        (response) =>
                        {
                            Assert.AreEqual(HttpStatusCode.OK, response.StatusCode, "Response status code should have been a 200.");

                            formatter.SetSerializer(type, new DataContractSerializer(type, new Type[] { obj.GetType() }));
                            string stringContent = response.Content.ReadAsString();
                            object readObj = response.Content.ReadAs(type, formatters);
                            TestDataAssert.AreEqual(obj, readObj, string.Format("Failed to round trip type '{0}'.  Content was:\r\n{1}.", type.Name, stringContent));
                        });
                });
        }

        #endregion ReadAs() from Http GET using DataContractSerializer with Xml

        #region ReadAs() from Http GET using DataContractSerializer with Json

        [Ignore]
        [TestMethod]
        [TestCategory("CIT"), Timeout(TimeoutConstant.ExtendedTimeout), Owner("dravva")]
        [Description("All value and reference types (including derived types) can be read using ReadAs() from Http GET using DataContractJsonSerializer.")]
        public void GetReadAsJson()
        {
            IEnumerable<TestData> testData = TestData.RepresentativeValueAndRefTypeTestDataCollection;
            TestDataVariations variations = TestDataVariations.AllNonInterfaces;
            XmlMediaTypeFormatter formatter = new XmlMediaTypeFormatter();
            MediaTypeFormatter[] formatters = new MediaTypeFormatter[] { formatter };

            TestDataAssert.Execute(
                testData,
                variations,
                "ObjectContent serializing using the Xml DataContractSerializer failed.",
                (type, obj) =>
                {
                    GenericWebService service = GenericWebService.GetServiceInstance(type, obj.GetType());
                    service.OnGetReturnInstance = () => obj;

                    HttpServiceHostAssert.Execute(
                        service,
                        CreateDataContractGetRequest(JsonMediaType),
                        (response) =>
                        {
                            Assert.AreEqual(HttpStatusCode.OK, response.StatusCode, "Response status code should have been a 200.");

                            formatter.SetSerializer(type, new DataContractSerializer(type, new Type[] { obj.GetType() }));
                            string stringContent = response.Content.ReadAsString();
                            object readObj = response.Content.ReadAs(type, formatters);
                            TestDataAssert.AreEqual(obj, readObj, string.Format("Failed to round trip type '{0}'.  Content was:\r\n{1}.", type.Name, stringContent));
                        });
                });
        }

        #endregion ReadAs() from Http GET using DataContractSerializer with Json


        #region ReadAs() from Http GET using XmlSerializer with Xml

        [TestMethod]
        [TestCategory("CIT"), Timeout(TimeoutConstant.ExtendedTimeout), Owner("dravva")]
        [Description("All value and reference types (without derived types) can be read ReadAs() from Http GET using XmlSerializer.")]
        public void GetReadAsXmlNoDerivedTypes()
        {
            IEnumerable<TestData> testData = TestData.RepresentativeValueAndRefTypeTestDataCollection;
            TestDataVariations variations = TestDataVariations.AllNonInterfaces & ~TestDataVariations.AsDerivedType;
            XmlMediaTypeFormatter formatter = new XmlMediaTypeFormatter();
            MediaTypeFormatter[] formatters = new MediaTypeFormatter[] { formatter };

            TestDataAssert.Execute(
                testData,
                variations,
                "ObjectContent serializing using the XmlSerializer failed.",
                (type, obj) =>
                {
                    GenericWebService service = GenericWebService.GetServiceInstance(type, obj.GetType());
                    service.OnGetReturnInstance = () => obj;

                    HttpServiceHostAssert.Execute(
                        service,
                        GenericWebService.GetRequest(),
                        (response) =>
                        {
                            Assert.AreEqual(HttpStatusCode.OK, response.StatusCode, "Response status code should have been a 200.");

                            formatter.SetSerializer(type, new XmlSerializer(type, new Type[] { obj.GetType() }));
                            object readObj = response.Content.ReadAs(type, formatters);
                            TestDataAssert.AreEqual(obj, readObj, string.Format("Failed to round trip type '{0}'.", type.Name));
                        });
                });
        }

        [Ignore]    
        [TestMethod]
        [TestCategory("CIT"), Timeout(TimeoutConstant.ExtendedTimeout), Owner("dravva")]
        [Description("All value and reference types (including derived types) can be read using ReadAs() from Http GET using DataContractSerializer for XML.")]
        public void GetReadAsXml()
        {
            IEnumerable<TestData> testData = TestData.RepresentativeValueAndRefTypeTestDataCollection;
            TestDataVariations variations = TestDataVariations.AllNonInterfaces;
            XmlMediaTypeFormatter formatter = new XmlMediaTypeFormatter();
            MediaTypeFormatter[] formatters = new MediaTypeFormatter[] { formatter };

            TestDataAssert.Execute(
                testData,
                variations,
                "ObjectContent serializing using the Xml DataContractSerializer failed.",
                (type, obj) =>
                {
                    GenericWebService service = GenericWebService.GetServiceInstance(type, obj.GetType());
                    service.OnGetReturnInstance = () => obj;

                    HttpServiceHostAssert.Execute(
                        service,
                        GenericWebService.GetRequest(),
                        (response) =>
                        {
                            Assert.AreEqual(HttpStatusCode.OK, response.StatusCode, "Response status code should have been a 200.");

                            formatter.SetSerializer(type, new DataContractSerializer(type, new Type[] { obj.GetType() }));
                            string stringContent = response.Content.ReadAsString();
                            object readObj = response.Content.ReadAs(type, formatters);
                            TestDataAssert.AreEqual(obj, readObj, string.Format("Failed to round trip type '{0}'.  Content was:\r\n{1}.", type.Name, stringContent));
                        });
                });
        }

        #endregion ReadAs() from Http GET using XmlSerializer with Xml

        #endregion ReadAs() from Http GET 

        #region Http POST of HttpRequestMessage<T> and ReadAs

        #region Http POST of HttpRequestMessage<T> and ReadAs using DataContractSerializer with Xml

        [Ignore]
        [TestMethod]
        [TestCategory("CIT"), Timeout(TimeoutConstant.ExtendedTimeout), Owner("dravva")]
        [Description("All value and reference types (including derived types) can be read using Http POST of HttpRequestMessage<T> and ReadAs using DataContractSerializer for XML.")]
        public void PostAndReadAsXmlDataContract()
        {
            IEnumerable<TestData> testData = TestData.RepresentativeValueAndRefTypeTestDataCollection;
            TestDataVariations variations = TestDataVariations.AllNonInterfaces;
            XmlMediaTypeFormatter formatter = new XmlMediaTypeFormatter();
            MediaTypeFormatter[] formatters = new MediaTypeFormatter[] { formatter };

            TestDataAssert.Execute(
                testData,
                variations,
                "ObjectContent serializing using the Xml DataContractSerializer failed.",
                (type, obj) =>
                {
                    GenericWebService service = GenericWebService.GetServiceInstance(type, obj.GetType(), useDataContract:true);

                    HttpServiceHostAssert.Execute(
                        service,
                        CreateDataContractPostRequestXml(type, obj),
                        (response) =>
                        {
                            Assert.AreEqual(HttpStatusCode.OK, response.StatusCode, "Response status code should have been a 200.");

                            formatter.SetSerializer(type, new DataContractSerializer(type, new Type[] { obj.GetType() }));
                            string stringContent = response.Content.ReadAsString();
                            object readObj = response.Content.ReadAs(type, formatters);
                            TestDataAssert.AreEqual(obj, readObj, string.Format("Failed to round trip type '{0}'.  Content was:\r\n{1}.", type.Name, stringContent));
                        });
                });
        }

        #endregion Http POST of HttpRequestMessage<T> and ReadAs using DataContractSerializer with Xml

        #region Http POST of HttpRequestMessage<T> and ReadAs using DataContractSerializer with Json

        [Ignore]
        [TestMethod]
        [TestCategory("CIT"), Timeout(TimeoutConstant.ExtendedTimeout), Owner("dravva")]
        [Description("All value and reference types (including derived types) can be read using Http POST of HttpRequestMessage<T> and ReadAs using DataContractJsonSerializer.")]
        public void PostAndReadAsJson()
        {
            IEnumerable<TestData> testData = TestData.RepresentativeValueAndRefTypeTestDataCollection;
            TestDataVariations variations = TestDataVariations.AllNonInterfaces;
            XmlMediaTypeFormatter formatter = new XmlMediaTypeFormatter();
            MediaTypeFormatter[] formatters = new MediaTypeFormatter[] { formatter };

            TestDataAssert.Execute(
                testData,
                variations,
                "ObjectContent serializing using the Json DataContractSerializer failed.",
                (type, obj) =>
                {
                    GenericWebService service = GenericWebService.GetServiceInstance(type, obj.GetType());

                    HttpServiceHostAssert.Execute(
                        service,
                        CreateDataContractPostRequestJson(type, obj),
                        (response) =>
                        {
                            Assert.AreEqual(HttpStatusCode.OK, response.StatusCode, "Response status code should have been a 200.");

                            formatter.SetSerializer(type, new DataContractSerializer(type, new Type[] { obj.GetType() }));
                            string stringContent = response.Content.ReadAsString();
                            object readObj = response.Content.ReadAs(type, formatters);
                            TestDataAssert.AreEqual(obj, readObj, string.Format("Failed to round trip type '{0}'.  Content was:\r\n{1}.", type.Name, stringContent));
                        });
                });
        }

        #endregion Http POST of HttpRequestMessage<T> and ReadAs using DataContractSerializer with Json

        #region Http POST of HttpRequestMessage<T> and ReadAs using XmlSerializer with Xml

        [Ignore]
        [TestMethod]
        [TestCategory("CIT"), Timeout(TimeoutConstant.ExtendedTimeout), Owner("dravva")]
        [Description("All value and reference types (without derived types) can be read Http POST of HttpRequestMessage<T> and ReadAs using XmlSerializer.")]
        public void PostAndReadAsXmlNoDerivedTypes()
        {
            IEnumerable<TestData> testData = TestData.RepresentativeValueAndRefTypeTestDataCollection;
            TestDataVariations variations = TestDataVariations.AllNonInterfaces & ~TestDataVariations.AsDerivedType;
            XmlMediaTypeFormatter formatter = new XmlMediaTypeFormatter();
            MediaTypeFormatter[] formatters = new MediaTypeFormatter[] { formatter };

            TestDataAssert.Execute(
                testData,
                variations,
                "ObjectContent serializing using the XmlSerializer failed.",
                (type, obj) =>
                {
                    GenericWebService service = GenericWebService.GetServiceInstance(type, obj.GetType());
                    HttpServiceHostAssert.Execute(
                        service,
                        CreateXmlSerializerPostRequest(type, obj, XmlMediaType),
                        (response) =>
                        {
                            Assert.AreEqual(HttpStatusCode.OK, response.StatusCode, "Response status code should have been a 200.");

                            formatter.SetSerializer(type, new XmlSerializer(type, new Type[] { obj.GetType() }));
                            object readObj = response.Content.ReadAs(type, formatters);
                            TestDataAssert.AreEqual(obj, readObj, string.Format("Failed to round trip type '{0}'.", type.Name));
                        });
                });
        }

        [Ignore]    
        [TestMethod]
        [TestCategory("CIT"), Timeout(TimeoutConstant.ExtendedTimeout), Owner("dravva")]
        [Description("All value and reference types (including derived types) can be read using Http POST of HttpRequestMessage<T> and ReadAs using XmlSerializer for XML.")]
        public void PostAndReadAsXml()
        {
            IEnumerable<TestData> testData = TestData.RepresentativeValueAndRefTypeTestDataCollection;
            TestDataVariations variations = TestDataVariations.AllNonInterfaces;
            XmlMediaTypeFormatter formatter = new XmlMediaTypeFormatter();
            MediaTypeFormatter[] formatters = new MediaTypeFormatter[] { formatter };

            TestDataAssert.Execute(
                testData,
                variations,
                "ObjectContent serializing using the Xml DataContractSerializer failed.",
                (type, obj) =>
                {
                    GenericWebService service = GenericWebService.GetServiceInstance(type, obj.GetType());
                    HttpServiceHostAssert.Execute(
                        service,
                        CreateXmlSerializerPostRequest(type, obj, XmlMediaType),
                        (response) =>
                        {
                            Assert.AreEqual(HttpStatusCode.OK, response.StatusCode, "Response status code should have been a 200.");

                            formatter.SetSerializer(type, new DataContractSerializer(type, new Type[] { obj.GetType() }));
                            string stringContent = response.Content.ReadAsString();
                            object readObj = response.Content.ReadAs(type, formatters);
                            TestDataAssert.AreEqual(obj, readObj, string.Format("Failed to round trip type '{0}'.  Content was:\r\n{1}.", type.Name, stringContent));
                        });
                });
        }

        #endregion Http POST of HttpRequestMessage<T> and ReadAs using XmlSerializer with Xml

        #endregion Http POST of HttpRequestMessage<T> and ReadAs 

        #endregion Acceptance Scenarios

        #region Test helpers

        public static HttpRequestMessage<T> CreateGenericGetRequest<T>(T obj, Uri uri, IEnumerable<MediaTypeFormatter> formatters)
        {
            // new Uri(GenericWebService.DataContractUri, UriKind.Relative)
            return new HttpRequestMessage<T>(obj, HttpMethod.Get, uri, formatters);
        }

        public static HttpRequestMessage CreateGenericRequest(Type type, object objectInstance, HttpMethod httpMethod, Uri uri, IEnumerable<MediaTypeFormatter> formatters)
        {
            return GenericTypeAssert.InvokeConstructor(
                    typeof(HttpRequestMessage<>),
                    type,
                    new Type[] { type, typeof(HttpMethod), typeof(Uri), typeof(IEnumerable<MediaTypeFormatter>) },
                    new object[] { objectInstance, httpMethod, uri, formatters }) as HttpRequestMessage;
        }

        public static HttpRequestMessage CreateDataContractGetRequest(MediaTypeWithQualityHeaderValue mediaType)
        {
            HttpRequestMessage request = GenericWebService.GetRequest();
            request.Headers.Accept.Add(mediaType);
            return request;
        }

        public static HttpRequestMessage CreateXmlSerializerGetRequest(Type type, object objectInstance)
        {
            return CreateGenericRequest(type, objectInstance, HttpMethod.Get, new Uri(GenericWebService.UriTemplate, UriKind.Relative), new MediaTypeFormatter[0]);
        }

        public static HttpRequestMessage CreateDataContractPostRequestXml(Type type, object objectInstance)
        {
            HttpRequestMessage request = CreateGenericRequest(type, objectInstance, HttpMethod.Post, new Uri(GenericWebService.UriTemplate, UriKind.Relative), new MediaTypeFormatter[0]);
            Assert.IsNotNull(request.Content, "Content was not set.");
            ObjectContent content = request.Content as ObjectContent;
            Assert.IsNotNull(content, "Content should have been ObjectContent.");
            content.Formatters.XmlFormatter.SetSerializer(type, new DataContractSerializer(type));
            request.Content.Headers.ContentType = XmlMediaType;
            request.Headers.Accept.Add(XmlMediaType);
            return request;
        }

        public static HttpRequestMessage CreateDataContractPostRequestJson(Type type, object objectInstance)
        {
            HttpRequestMessage request = CreateGenericRequest(type, objectInstance, HttpMethod.Post, new Uri(GenericWebService.UriTemplate, UriKind.Relative), new MediaTypeFormatter[0]);
            Assert.IsNotNull(request.Content, "Content was not set.");
            ObjectContent content = request.Content as ObjectContent;
            Assert.IsNotNull(content, "Content should have been ObjectContent.");
            // $$$ content.Formatters.XmlFormatter.SetSerializer(type, new DataContractJsonSerializer(type));
            request.Content.Headers.ContentType = JsonMediaType;
            request.Headers.Accept.Add(JsonMediaType);
            return request;
        }

        public static HttpRequestMessage CreateXmlSerializerPostRequest(Type type, object objectInstance, MediaTypeWithQualityHeaderValue mediaType)
        {
            HttpRequestMessage request = CreateGenericRequest(type, objectInstance, HttpMethod.Post, new Uri(GenericWebService.UriTemplate, UriKind.Relative), new MediaTypeFormatter[0]);
            Assert.IsNotNull(request.Content, "Content was not set.");
            ObjectContent content = request.Content as ObjectContent;
            Assert.IsNotNull(content, "Content should have been ObjectContent.");
            content.Formatters.XmlFormatter.SetSerializer(type, new XmlSerializer(type));
            request.Content.Headers.ContentType = mediaType;
            request.Headers.Accept.Add(mediaType);
            return request;
        }

        #endregion Test helpers

        #region Test types

        public class MyPocoType
        {
            public int Id { get; set; }
            public string Name { get; set; }
        }

        #endregion Test types
    }
}
