namespace Microsoft.ServiceModel.Web.Test
{
    using System;
    using System.Collections.Generic;
    using System.Globalization;
    using System.IO;
    using System.Net;
    using System.Text;
    using Microsoft.VisualStudio.TestTools.UnitTesting;

    /// <summary>
    /// Tests for endpoints created using the <see cref="WebHttpBehavior3"/> behavior.
    /// </summary>
    [TestClass]
    public class WebHttpBehavior3Tests
    {
        internal const string FormUrlEncodedContentType = "application/x-www-form-urlencoded";
        internal const string ApplicationJsonContentTypeWithCharset = "application/json; charset=utf-8";
        internal const string Endpoint = "http://localhost:8080/JQueryWCFService";
        static readonly Encoding UTF8Encoding = new UTF8Encoding(false);
        static WebServiceHost3 jQueryServiceHost;

        /// <summary>
        /// Callback method, called in the beginning of the test to set up the service used for tests.
        /// </summary>
        [TestInitialize]
        public void MyTestInitialize()
        {
            jQueryServiceHost = new WebServiceHost3(typeof(JQueryWCFService), new Uri(Endpoint));
            jQueryServiceHost.Open();
        }

        /// <summary>
        /// Callback method, called in the end of the test to tear down the service used for tests.
        /// </summary>
        [TestCleanup]
        public void MyClassCleanup()
        {
            try
            {
                jQueryServiceHost.Close();
            }
            finally
            {
                if (jQueryServiceHost.State != System.ServiceModel.CommunicationState.Closed)
                {
                    jQueryServiceHost.Abort();
                }
            }
        }

        /// <summary>
        /// Simple test for POST calls.
        /// </summary>
        [TestMethod]
        public void WebInvokeTest()
        {
            WebInvokeTestWithEncoding(UTF8Encoding, FormUrlEncodedContentType);
        }

        /// <summary>
        /// Simple test for POST calls, with UTF-16LE-encoded body.
        /// </summary>
        [TestMethod]
        public void WebInvokeTestUTF16()
        {
            UnicodeEncoding encoding = new UnicodeEncoding(false, false);
            WebInvokeTestWithEncoding(encoding, FormUrlEncodedContentType + "; charset=utf-16");
        }

        /// <summary>
        /// Simple test for POST calls, with UTF-16BE-encoded body.
        /// </summary>
        [TestMethod]
        public void WebInvokeTestUTF16BigEndian()
        {
            UnicodeEncoding encoding = new UnicodeEncoding(true, false);
            WebInvokeTestWithEncoding(encoding, FormUrlEncodedContentType + "; charset=utf-16BE");
        }

        /// <summary>
        /// Simple test for GET calls.
        /// </summary>
        [TestMethod]
        public void WebGetTest()
        {
            string queryString = "/JQueryGet/Foo/Bar?Address=Capital+Hill&customer%5BName%5D=Pete&customer%5BAddress%5D=Redmond&" +
                "customer%5BAge%5D%5B0%5D%5B%5D=23&customer%5BAge%5D%5B0%5D%5B%5D=24&customer%5BAge%5D%5B1%5D%5B%5D=25&" +
                "customer%5BAge%5D%5B1%5D%5B%5D=26&customer%5BPhones%5D%5B%5D=425+888+1111&customer%5BPhones%5D%5B%5D=425+345+7777&" +
                "customer%5BPhones%5D%5B%5D=425+888+4564&customer%5BEnrolmentDate%5D=%2FDate(1277243030667)%2F&role=PM&changeDate=3&count=15";
            string expectedReturn = @"{""Name"":""Yavor"",""Address"":""Capital Hill"",""Age"":[[""23"",""24""],[""25"",""26""]],""Phones"":[""425 888 1111"",""425 345 7777"",""425 888 4564""],""EnrolmentDate"":""\/Date(1277243030667)\/""}";

            WebHttpBehavior3Tests.Test("GET", Endpoint + queryString, null, null, HttpStatusCode.OK, ApplicationJsonContentTypeWithCharset, expectedReturn);
        }

        /// <summary>
        /// Simple test for GET calls for operations which use the operation context.
        /// </summary>
        [TestMethod]
        public void WebGetWithWebOperationContext()
        {
            WebHttpBehavior3Tests.Test("GET", Endpoint + "/GetSettingHttpHeaders?X-Foo=something", null, null, HttpStatusCode.OK, ApplicationJsonContentTypeWithCharset, "{\"X-Foo\":\"something\"}");
        }

        /// <summary>
        /// Simple test for POST calls for operations which use the operation context.
        /// </summary>
        [TestMethod]
        public void WebInvokeWithWebOperationContext()
        {
            WebHttpBehavior3Tests.Test("POST", Endpoint + "/PostSettingHttpHeaders", FormUrlEncodedContentType, "Cache-Control=no-cache", HttpStatusCode.OK, ApplicationJsonContentTypeWithCharset, "{\"Cache-Control\":\"no-cache\"}");
        }

        /// <summary>
        /// Simple test for POST calls for operations which use the operation context to change the response content-type.
        /// </summary>
        [TestMethod]
        public void WebInvokeWithWebOperationContextChangingContentType()
        {
            WebHttpBehavior3Tests.Test("POST", Endpoint + "/PostSettingHttpHeaders", FormUrlEncodedContentType, "Content-Type=text%2Fplain", HttpStatusCode.OK, "text/plain", "{\"Content-Type\":\"text\\/plain\"}");
        }

        /// <summary>
        /// Simple test to ensure that the <see cref="WebHttpBehavior3"/> can be used for parameters other than <see cref="JsonValue"/>.
        /// </summary>
        [TestMethod]
        public void TestJsonInputWithNoJsonValue()
        {
            int x, y;
            string expectedJson, expectedXml;

            CreateInputValuesAndExpectedResults(1, out x, out y, out expectedJson, out expectedXml);

            string json = String.Format(CultureInfo.InvariantCulture, "{{\"x\":{0}, \"y\":{1}}}", x, y);

            WebHttpBehavior3Tests.Test("POST", Endpoint + "/AddJson", "application/json", json, HttpStatusCode.OK, ApplicationJsonContentTypeWithCharset, expectedJson);
            WebHttpBehavior3Tests.Test("POST", Endpoint + "/AddXml", "application/json", json, HttpStatusCode.OK, ApplicationJsonContentTypeWithCharset, expectedJson);
        }

        /// <summary>
        /// Simple test to ensure that the <see cref="WebHttpBehavior3"/> can be used for parameters other than <see cref="JsonValue"/>.
        /// </summary>
        [TestMethod]
        public void TestXmlInputWithNoJsonValue()
        {
            int x, y;
            string expectedJson, expectedXml;

            CreateInputValuesAndExpectedResults(2, out x, out y, out expectedJson, out expectedXml);

            string addXmlInput = String.Format(CultureInfo.InvariantCulture, "<AddXml xmlns=\"http://tempuri.org/\"><x>{0}</x><y>{1}</y></AddXml>", x, y);
            string addJsonInput = addXmlInput.Replace("AddXml", "AddJson");

            WebHttpBehavior3Tests.Test("POST", Endpoint + "/AddJson", "text/xml", addJsonInput, HttpStatusCode.OK, "text/xml; charset=utf-8", expectedXml);
            WebHttpBehavior3Tests.Test("POST", Endpoint + "/AddXml", "text/xml", addXmlInput, HttpStatusCode.OK, "text/xml; charset=utf-8", expectedXml);
        }

        [TestMethod]
        public void TestJsonInputWithJsonValue()
        {
            List<string> jsonInputs = new List<string>
            {
                "null",
                "123",
                "123.45",
                "\"hello\"",
                "true",
                "false",
                "[]",
                "{}",
                "[1,2,3]",
                "[[[1],[2,2]],3,[4,5,6]]",
                "{\"a\":123,\"b\":null,\"c\":[1,2],\"d\":{\"e\":1}}",
            };

            string[] contentTypes = new string[]
            {
                "text/json",
                "text/json; charset=utf-8",
                "application/json",
                ApplicationJsonContentTypeWithCharset,
            };

            int contentTypeIndex = 0;

            foreach (string json in jsonInputs)
            {
                string contentType = contentTypes[contentTypeIndex];
                Console.WriteLine("Sending JSON {0} with content-type {1}", json, contentType);
                WebHttpBehavior3Tests.Test("POST", Endpoint + "/Echo", contentType, json, HttpStatusCode.OK, json == "null" ? "" : ApplicationJsonContentTypeWithCharset, json == "null" ? "" : json);
                contentTypeIndex = (contentTypeIndex + 1) % contentTypes.Length;
            }
        }

        [TestMethod]
        public void TestJsonValueNotLastParameter()
        {
            WebHttpBehavior3Tests.Test("POST", Endpoint + "/GetKeyValue/b", FormUrlEncodedContentType, "a=1&b=2", HttpStatusCode.OK, ApplicationJsonContentTypeWithCharset, "\"2\"");
        }

        [TestMethod]
        public void TestReturnTypeNotJsonValue()
        {
            Test("POST", Endpoint + "/GetArrayMember/a", FormUrlEncodedContentType, "a[]=1&a[]=2", HttpStatusCode.OK, ApplicationJsonContentTypeWithCharset, "[\"1\",\"2\"]");
            Test("POST", Endpoint + "/GetObjectMember/a", FormUrlEncodedContentType, "a[a]=1&a[b]=2", HttpStatusCode.OK, ApplicationJsonContentTypeWithCharset, "{\"a\":\"1\",\"b\":\"2\"}");
            Test("POST", Endpoint + "/GetPrimitiveMember/a", FormUrlEncodedContentType, "a=hello", HttpStatusCode.OK, ApplicationJsonContentTypeWithCharset, "\"hello\"");
        }

        [TestMethod]
        public void TestInvalidContentType()
        {
            HttpWebResponse httpResponse = SendRequest("POST", Endpoint + "/Echo", "text/plain", "{\"a\":123}", Encoding.UTF8, null);
            Assert.AreEqual<HttpStatusCode>(HttpStatusCode.BadRequest, httpResponse.StatusCode);
        }

        internal static void Test(string method, string address, string contentType, string body, HttpStatusCode expectedHttpStatus, string expectedResponseContentType, string expectedResponseBody)
        {
            Test(method, address, contentType, body, UTF8Encoding, expectedHttpStatus, expectedResponseContentType, expectedResponseBody);
        }

        internal static void Test(string method, string address, string contentType, string body, Encoding bodyEncoding, HttpStatusCode expectedHttpStatus, string expectedResponseContentType, string expectedResponseBody)
        {
            HttpWebResponse httpResponse = SendRequest(method, address, contentType, body, bodyEncoding, null);
            ValidateHttpResponse(httpResponse, expectedHttpStatus, expectedResponseContentType, expectedResponseBody);
        }

        internal static HttpWebResponse SendRequest(string method, string address, string contentType, string body, Encoding bodyEncoding, Dictionary<string, string> additionalHeaders)
        {
            HttpWebRequest httpRequest = (HttpWebRequest)WebRequest.Create(address);
            httpRequest.Method = method;
            if (contentType != null)
            {
                httpRequest.ContentType = contentType;
            }

            if (additionalHeaders != null)
            {
                foreach (string headerName in additionalHeaders.Keys)
                {
                    switch (headerName)
                    {
                        case "Accept":
                            httpRequest.Accept = additionalHeaders[headerName];
                            break;
                        default:
                            httpRequest.Headers[headerName] = additionalHeaders[headerName];
                            break;
                    }
                }
            }

            if (body != null)
            {
                using (Stream stream = httpRequest.GetRequestStream())
                {
                    byte[] bytes = bodyEncoding.GetBytes(body);
                    stream.Write(bytes, 0, bytes.Length);
                    stream.Flush();
                }
            }

            HttpWebResponse httpResponse;
            try
            {
                httpResponse = (HttpWebResponse)httpRequest.GetResponse();
            }
            catch (WebException e)
            {
                httpResponse = (HttpWebResponse)e.Response;
            }

            return httpResponse;
        }

        internal static void ValidateHttpResponse(HttpWebResponse httpResponse, HttpStatusCode expectedHttpStatus, string expectedResponseContentType, string expectedResponseBody)
        {
            Assert.IsNotNull(httpResponse);
            Assert.AreEqual<HttpStatusCode>(expectedHttpStatus, httpResponse.StatusCode);
            if (expectedResponseContentType != null)
            {
                Assert.AreEqual<string>(expectedResponseContentType, httpResponse.ContentType);
            }

            if (expectedResponseBody == null)
            {
                Assert.AreEqual(0, httpResponse.ContentLength);
            }
            else
            {
                Encoding responseEncoding = GetResponseEncoding(httpResponse.ContentType);
                StreamReader reader;
                if (responseEncoding == null)
                {
                    reader = new StreamReader(httpResponse.GetResponseStream());
                }
                else
                {
                    reader = new StreamReader(httpResponse.GetResponseStream(), responseEncoding);
                }

                string contents = reader.ReadToEnd();
                Assert.AreEqual(expectedResponseBody, contents);
                reader.Dispose();
            }
        }

        internal static void CreateInputValuesAndExpectedResults(int seed, out int x, out int y, out string expectedJson, out string expectedXml)
        {
            Random rndGen = new Random(seed);
            x = rndGen.Next(-10000, 10000);
            y = rndGen.Next(-10000, 10000);
            int result = x + y;
            Console.WriteLine("x={0},y={1},result={2}", x, y, result);

            expectedJson = result.ToString(CultureInfo.InvariantCulture);
            expectedXml = String.Format(
                CultureInfo.InvariantCulture,
                "<int xmlns=\"http://schemas.microsoft.com/2003/10/Serialization/\">{0}</int>",
                result);
        }

        static Encoding GetResponseEncoding(string contentType)
        {
            Encoding result = null;
            string charsetName = "charset=";
            int charsetIndex = contentType.IndexOf(charsetName);
            if (charsetIndex >= 0)
            {
                string charset = contentType.Substring(charsetIndex + charsetName.Length);
                return Encoding.GetEncoding(charset);
            }

            return result;
        }

        static void WebInvokeTestWithEncoding(Encoding encoding, string contentType)
        {
            string jsonString = @"customer%5BName%5D=Pete&customer%5BAddress%5D=Redmond&customer%5BAge%5D%5B0%5D%5B%5D=23&" +
                "customer%5BAge%5D%5B0%5D%5B%5D=24&customer%5BAge%5D%5B1%5D%5B%5D=25&customer%5BAge%5D%5B1%5D%5B%5D=26&" +
                "customer%5BPhones%5D%5B%5D=425+888+1111&customer%5BPhones%5D%5B%5D=425+345+7777&" +
                "customer%5BPhones%5D%5B%5D=425+888+4564&customer%5BEnrolmentDate%5D=%2FDate(1277243030664)%2F&" +
                "customers%5B0%5D%5BName%5D=Pete2&customers%5B0%5D%5BAddress%5D=Redmond2&customers%5B0%5D%5BAge%5D%5B0%5D%5B%5D=23&" +
                "customers%5B0%5D%5BAge%5D%5B0%5D%5B%5D=24&customers%5B0%5D%5BAge%5D%5B1%5D%5B%5D=25&" +
                "customers%5B0%5D%5BAge%5D%5B1%5D%5B%5D=26&customers%5B0%5D%5BPhones%5D%5B%5D=425+888+1111&" +
                "customers%5B0%5D%5BPhones%5D%5B%5D=425+345+7777&customers%5B0%5D%5BPhones%5D%5B%5D=425+888+4564&" +
                "customers%5B0%5D%5BEnrolmentDate%5D=%2FDate(1277243030664)%2F&customers%5B1%5D%5BName%5D=Pete3&" +
                "customers%5B1%5D%5BAddress%5D=Redmond3&customers%5B1%5D%5BAge%5D%5B0%5D%5B%5D=23&customers%5B1%5D%5BAge%5D%5B0%5D%5B%5D=24&" +
                "customers%5B1%5D%5BAge%5D%5B1%5D%5B%5D=25&customers%5B1%5D%5BAge%5D%5B1%5D%5B%5D=26&" +
                "customers%5B1%5D%5BPhones%5D%5B%5D=425+888+1111&customers%5B1%5D%5BPhones%5D%5B%5D=425+345+7777&" +
                "customers%5B1%5D%5BPhones%5D%5B%5D=425+888+4564&customers%5B1%5D%5BEnrolmentDate%5D=%2FDate(1277243030664)%2F&role=NewRole&changeDate=3&count=15";
            string expectedReturn = @"{""Name"":""Yavor"",""Address"":""Redmond"",""Age"":[[""23"",""24""],[""25"",""26""]],""Phones"":[""425 888 1111"",""425 345 7777"",""425 888 4564""],""EnrolmentDate"":""\/Date(1277243030664)\/""}";

            WebHttpBehavior3Tests.Test("POST", Endpoint + "/JQuery/Foo/Bar", contentType, jsonString, encoding, HttpStatusCode.OK, ApplicationJsonContentTypeWithCharset, expectedReturn);
        }
    }
}