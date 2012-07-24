namespace Microsoft.ServiceModel.Web.Test
{
    using System;
    using System.Collections.Generic;
    using System.Globalization;
    using System.Json;
    using System.Net;
    using System.ServiceModel;
    using System.ServiceModel.Web;
    using System.Text;
    using global::Microsoft.VisualStudio.TestTools.UnitTesting;

    [TestClass]
    public class WebHttpBehaviorProperties3Tests
    {
        const string JsonpContentType = "application/x-javascript";

        [ServiceContract]
        public interface ITest
        {
            [OperationContract]
            JsonValue EchoPost(JsonValue input);

            [OperationContract, WebInvoke]
            JsonValue EchoPostWithWebInvoke(JsonValue input);

            [OperationContract, WebGet]
            JsonValue EchoGet();
        }

        [ServiceContract]
        public interface IJsonpTest
        {
            [WebGet]
            JsonValue EchoGet();

            [WebGet(ResponseFormat = WebMessageFormat.Json)]
            int Add(int x, int y, int returnStatusCode);

            [WebGet]
            JsonValue Non200Return(int returnStatusCode);
        }

        [TestMethod]
        public void JsonValueReturnContentTypeIsJsonWithAllDefaultResponseFormats()
        {
            WebHttpBinding binding = new WebHttpBinding();
            WebMessageFormat[] defaultResponseFormats = new WebMessageFormat[] { WebMessageFormat.Json, WebMessageFormat.Xml };
            foreach (WebMessageFormat format in defaultResponseFormats)
            {
                WebHttpBehavior3 behavior = new WebHttpBehavior3 { DefaultOutgoingResponseFormat = format };
                Test(
                    binding,
                    behavior,
                    "POST",
                    WebHttpBehavior3Tests.Endpoint + "/EchoPost",
                    WebHttpBehavior3Tests.FormUrlEncodedContentType,
                    "a=1&b=2",
                    HttpStatusCode.OK,
                    WebHttpBehavior3Tests.ApplicationJsonContentTypeWithCharset,
                    "{\"a\":\"1\",\"b\":\"2\"}");
                Test(
                    binding,
                    behavior,
                    "POST",
                    WebHttpBehavior3Tests.Endpoint + "/EchoPostWithWebInvoke",
                    WebHttpBehavior3Tests.FormUrlEncodedContentType,
                    "a=1&b=2",
                    HttpStatusCode.OK,
                    WebHttpBehavior3Tests.ApplicationJsonContentTypeWithCharset,
                    "{\"a\":\"1\",\"b\":\"2\"}");
                Test(
                    binding,
                    behavior,
                    "GET",
                    WebHttpBehavior3Tests.Endpoint + "/EchoGet?a=1&b=2",
                    null,
                    null,
                    HttpStatusCode.OK,
                    WebHttpBehavior3Tests.ApplicationJsonContentTypeWithCharset,
                    "{\"a\":\"1\",\"b\":\"2\"}");
            }
        }

        [TestMethod]
        public void BasicJsonpTest()
        {
            JsonpTests<JsonpTestService, IJsonpTest>(
                "/EchoGet?a=1&b=2&callback=MyFunc",
                HttpStatusCode.OK,
                JsonpContentType,
                "MyFunc({\"a\":\"1\",\"b\":\"2\"});");
        }

        [TestMethod]
        public void JsonpFromNonJsonValueTest()
        {
            JsonpTests<JsonpTestService, IJsonpTest>(
                "/Add?x=1&y=2&callback=MyFunc&returnStatusCode=200",
                HttpStatusCode.OK,
                JsonpContentType,
                "MyFunc(3);");
        }

        [TestMethod]
        public void JsonpWithDifferentCallbackParamNameTest()
        {
            JsonpTests<JsonpWithDifferentCallbackNameTestService, IJsonpTest>(
                "/EchoGet?a=1&b=2&" + JsonpWithDifferentCallbackNameTestService.CallbackParamName + "=MyFunc",
                HttpStatusCode.OK,
                JsonpContentType,
                "MyFunc({\"a\":\"1\",\"b\":\"2\"});");
        }

        [TestMethod]
        public void JsonpWithDifferentStatusCodeAccepted()
        {
            int statusCode = 202;
            JsonpTests<JsonpTestService, IJsonpTest>(
                "/Add?x=1&y=2&callback=MyFunc&returnStatusCode=" + statusCode,
                HttpStatusCode.OK,
                JsonpContentType,
                "MyFunc(3," + statusCode + ");");
        }

        [TestMethod]
        public void JsonpWithDifferentStatusCodeCreated()
        {
            int statusCode = (int)HttpStatusCode.Created;
            JsonpTests<JsonpTestService, IJsonpTest>(
                "/Add?x=1&y=2&callback=MyFunc&returnStatusCode=" + statusCode,
                HttpStatusCode.OK,
                JsonpContentType,
                "MyFunc(3," + statusCode + ");");
        }

        [TestMethod]
        public void JsonpWithIntlFunctionName()
        {
            string[] funcNames = new string[]
            {
                "Latin-ãêíôöù",
                "Japanese-日本語",
                "Chinese-我说中文",
            };

            foreach (string funcName in funcNames)
            {
                JsonpTests<JsonpTestService, IJsonpTest>(
                    "/EchoGet?a=1&b=2&callback=" + funcName,
                    HttpStatusCode.OK,
                    JsonpContentType,
                    funcName + "({\"a\":\"1\",\"b\":\"2\"});");
            }
        }

        [TestMethod]
        public void JsonpOnPost()
        {
            WebHttpBinding binding = new WebHttpBinding();
            binding.CrossDomainScriptAccessEnabled = true;
            WebHttpBehavior3 behavior = new WebHttpBehavior3();
            Test(
                binding,
                behavior,
                "POST",
                WebHttpBehavior3Tests.Endpoint + "/EchoPost?callback=MyFunc",
                WebHttpBehavior3Tests.FormUrlEncodedContentType,
                "a=1&b=2",
                HttpStatusCode.OK,
                JsonpContentType,
                "MyFunc({\"a\":\"1\",\"b\":\"2\"});");
        }

        [TestMethod]
        public void TestAutomaticFormatSelectionFromInput()
        {
            int x, y;
            string expectedJson, expectedXml;

            WebHttpBehavior3Tests.CreateInputValuesAndExpectedResults(3, out x, out y, out expectedJson, out expectedXml);

            string xmlInput = String.Format(CultureInfo.InvariantCulture, "<AddJsonOrXml xmlns=\"http://tempuri.org/\"><x>{0}</x><y>{1}</y></AddJsonOrXml>", x, y);
            string jsonInput = String.Format(CultureInfo.InvariantCulture, "{{\"x\":{0}, \"y\":{1}}}", x, y);

            WebHttpBinding binding = new WebHttpBinding();
            WebHttpBehavior3 behavior = new WebHttpBehavior3();
            behavior.AutomaticFormatSelectionEnabled = true;

            Test<JQueryWCFService, IJQueryWCF>(binding, behavior, "POST", WebHttpBehavior3Tests.Endpoint + "/AddJsonOrXml", "application/xml; charset=utf-8", xmlInput, HttpStatusCode.OK, "application/xml; charset=utf-8", expectedXml);
            Test<JQueryWCFService, IJQueryWCF>(binding, behavior, "POST", WebHttpBehavior3Tests.Endpoint + "/AddJsonOrXml", "application/json", jsonInput, HttpStatusCode.OK, WebHttpBehavior3Tests.ApplicationJsonContentTypeWithCharset, expectedJson);
        }

        [TestMethod]
        public void TestAutomaticFormatSelectionFromAcceptHeader()
        {
            int x, y;
            string expectedJson, expectedXml;

            WebHttpBehavior3Tests.CreateInputValuesAndExpectedResults(3, out x, out y, out expectedJson, out expectedXml);

            string xmlInput = String.Format(CultureInfo.InvariantCulture, "<AddJsonOrXml xmlns=\"http://tempuri.org/\"><x>{0}</x><y>{1}</y></AddJsonOrXml>", x, y);
            string jsonInput = String.Format(CultureInfo.InvariantCulture, "{{\"x\":{0}, \"y\":{1}}}", x, y);

            WebHttpBinding binding = new WebHttpBinding();
            WebHttpBehavior3 behavior = new WebHttpBehavior3();
            behavior.AutomaticFormatSelectionEnabled = true;

            using (ServiceHost host = new ServiceHost(typeof(JQueryWCFService), new Uri(WebHttpBehavior3Tests.Endpoint)))
            {
                host.AddServiceEndpoint(typeof(IJQueryWCF), binding, "").Behaviors.Add(behavior);
                try
                {
                    host.Open();

                    foreach (bool useJsonInput in new bool[] { false, true })
                    {
                        foreach (bool useAcceptJson in new bool[] { false, true })
                        {
                            string input = useJsonInput ? jsonInput : xmlInput;
                            string requestContentType = useJsonInput ? "application/json" : "application/xml";
                            string expectedResponseContentType = useAcceptJson ? WebHttpBehavior3Tests.ApplicationJsonContentTypeWithCharset : "application/xml; charset=utf-8";
                            string expectedResponseBody = useAcceptJson ? expectedJson : expectedXml;
                            string acceptHeader = useAcceptJson ? "application/json" : "application/xml";

                            Console.WriteLine("Sending {0} request with Accept: {1}", requestContentType, acceptHeader);
                            Dictionary<string, string> headers = new Dictionary<string, string>();
                            headers.Add("Accept", acceptHeader);

                            string address = WebHttpBehavior3Tests.Endpoint + "/AddJsonOrXml";
                            HttpWebResponse httpResponse = WebHttpBehavior3Tests.SendRequest("POST", address, requestContentType, input, Encoding.UTF8, headers);
                            WebHttpBehavior3Tests.ValidateHttpResponse(httpResponse, HttpStatusCode.OK, expectedResponseContentType, expectedResponseBody);
                        }
                    }
                }
                catch
                {
                    host.Abort();
                    throw;
                }
            }
        }

        static void JsonpTests<TService, IContract>(string relativeAddress, HttpStatusCode expectedHttpStatus, string expectedResponseContentType, string expectedResponseBody)
        {
            WebHttpBinding binding = new WebHttpBinding();
            binding.CrossDomainScriptAccessEnabled = true;
            WebHttpBehavior3 behavior = new WebHttpBehavior3();

            Test<TService, IContract>(
                binding,
                behavior,
                "GET",
                WebHttpBehavior3Tests.Endpoint + relativeAddress,
                null,
                null,
                expectedHttpStatus,
                expectedResponseContentType,
                expectedResponseBody);
        }

        static void Test(WebHttpBinding binding, WebHttpBehavior3 behavior, string method, string address, string contentType, string body, HttpStatusCode expectedHttpStatus, string expectedResponseContentType, string expectedResponseBody)
        {
            Test<Service, ITest>(binding, behavior, method, address, contentType, body, expectedHttpStatus, expectedResponseContentType, expectedResponseBody);
        }

        static void Test<TService, IContract>(WebHttpBinding binding, WebHttpBehavior3 behavior, string method, string address, string contentType, string body, HttpStatusCode expectedHttpStatus, string expectedResponseContentType, string expectedResponseBody)
        {
            using (ServiceHost host = new ServiceHost(typeof(TService), new Uri(WebHttpBehavior3Tests.Endpoint)))
            {
                host.AddServiceEndpoint(typeof(IContract), binding, "").Behaviors.Add(behavior);
                try
                {
                    host.Open();
                    WebHttpBehavior3Tests.Test(method, address, contentType, body, expectedHttpStatus, expectedResponseContentType, expectedResponseBody);
                }
                catch
                {
                    host.Abort();
                    throw;
                }
            }
        }

        public class Service : ITest
        {
            public JsonValue EchoPost(JsonValue input)
            {
                return input;
            }

            public JsonValue EchoPostWithWebInvoke(JsonValue input)
            {
                return input;
            }

            public JsonValue EchoGet()
            {
                return WebOperationContext.Current.IncomingRequest.GetQueryStringAsJsonObject();
            }
        }

        public class JsonpTestService : IJsonpTest
        {
            public JsonValue EchoGet()
            {
                JsonObject result = (JsonObject)WebOperationContext.Current.IncomingRequest.GetQueryStringAsJsonObject();
                result.Remove("callback");
                return result;
            }

            public int Add(int x, int y, int returnStatusCode)
            {
                HttpStatusCode statusCode = (HttpStatusCode)returnStatusCode;
                switch (statusCode)
                {
                    case HttpStatusCode.Created:
                        var queryParams = WebOperationContext.Current.IncomingRequest.UriTemplateMatch.QueryParameters;
                        string uri = queryParams["uri"] ?? "http://tempuri.org/";
                        WebOperationContext.Current.OutgoingResponse.SetStatusAsCreated(new Uri(uri));
                        break;
                    case HttpStatusCode.NotFound:
                        WebOperationContext.Current.OutgoingResponse.SetStatusAsNotFound();
                        break;
                    default:
                        WebOperationContext.Current.OutgoingResponse.StatusCode = statusCode;
                        break;
                }

                return x + y;
            }

            public JsonValue Non200Return(int returnStatusCode)
            {
                JsonObject obj = (JsonObject)WebOperationContext.Current.IncomingRequest.GetQueryStringAsJsonObject();
                obj.Remove("returnStatusCode");
                obj.Remove("callback");
                WebOperationContext.Current.OutgoingResponse.StatusCode = (HttpStatusCode)returnStatusCode;
                return obj;
            }
        }

        [JavascriptCallbackBehavior(UrlParameterName = JsonpWithDifferentCallbackNameTestService.CallbackParamName)]
        public class JsonpWithDifferentCallbackNameTestService : IJsonpTest
        {
            internal const string CallbackParamName = "MyNewCallbackName";
            static readonly JsonpTestService service = new JsonpTestService();

            public JsonValue EchoGet()
            {
                JsonObject result = (JsonObject)WebOperationContext.Current.IncomingRequest.GetQueryStringAsJsonObject();
                result.Remove("MyNewCallbackName");
                return result;
            }

            public int Add(int x, int y, int returnStatusCode)
            {
                return service.Add(x, y, returnStatusCode);
            }

            public JsonValue Non200Return(int returnStatusCode)
            {
                JsonObject obj = (JsonObject)WebOperationContext.Current.IncomingRequest.GetQueryStringAsJsonObject();
                obj.Remove("returnStatusCode");
                obj.Remove("MyNewCallbackName");
                WebOperationContext.Current.OutgoingResponse.StatusCode = (HttpStatusCode)returnStatusCode;
                return obj;
            }
        }
    }
}
