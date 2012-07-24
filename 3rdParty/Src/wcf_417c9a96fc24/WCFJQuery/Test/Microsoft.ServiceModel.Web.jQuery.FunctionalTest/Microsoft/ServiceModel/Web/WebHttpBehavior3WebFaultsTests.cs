namespace Microsoft.ServiceModel.Web.Test
{
    using System;
    using System.Json;
    using System.Net;
    using System.Reflection;
    using System.ServiceModel;
    using System.ServiceModel.Web;
    using Microsoft.Silverlight.Cdf.Test.Common.Utility;
    using Microsoft.VisualStudio.TestTools.UnitTesting;

    [TestClass]
    public class WebHttpBehavior3WebFaultTests
    {
        [ServiceContract]
        public interface ITest
        {
            [WebGet]
            JsonValue ThrowWebFaultException(int statusCode);

            [WebInvoke(BodyStyle = WebMessageBodyStyle.WrappedRequest, ResponseFormat = WebMessageFormat.Json)]
            JsonValue ThrowWebFaultExceptionOfT(int statusCode, string detail);

            [WebInvoke(UriTemplate = "/ThrowWebFaultExceptionOfJsonValue?statusCode={statusCode}&useExactJsonValueType={useExactJsonValueType}")]
            JsonValue ThrowWebFaultExceptionOfJsonValue(int statusCode, bool useExactJsonValueType, JsonValue detail);

            [WebInvoke(UriTemplate = "/ThrowWebFaultExceptionOfJsonValueChangingContentType?statusCode={statusCode}")]
            JsonValue ThrowWebFaultExceptionOfJsonValueChangingContentType(int statusCode, JsonValue detail);
        }

        [TestMethod]
        public void NormalWebFaultException()
        {
            WebHttpBinding binding = new WebHttpBinding();
            WebHttpBehavior3 behavior = new WebHttpBehavior3();
            behavior.FaultExceptionEnabled = false;

            HttpStatusCode[] statusCodes = new HttpStatusCode[] { HttpStatusCode.BadRequest, HttpStatusCode.InternalServerError, HttpStatusCode.PreconditionFailed };
            foreach (HttpStatusCode statusCode in statusCodes)
            {
                Test(binding, behavior, "GET", WebHttpBehavior3Tests.Endpoint + "/ThrowWebFaultException?statusCode=" + (int)statusCode, null, null, statusCode, null, null);
            }
        }

        [TestMethod]
        public void WebFaultExceptionOfT()
        {
            int seed = MethodBase.GetCurrentMethod().Name.GetHashCode();
            Random rndGen = new Random(seed);
            WebHttpBinding binding = new WebHttpBinding();
            WebHttpBehavior3 behavior = new WebHttpBehavior3();
            behavior.FaultExceptionEnabled = false;

            HttpStatusCode[] statusCodes = new HttpStatusCode[] { HttpStatusCode.Unauthorized, HttpStatusCode.ServiceUnavailable, HttpStatusCode.RequestTimeout };
            foreach (HttpStatusCode statusCode in statusCodes)
            {
                string detail = PrimitiveCreator.CreateRandomString(rndGen, 30, "abcdefghijklmnopqrstuvwxyz");
                string requestBody = "{\"statusCode\":" + (int)statusCode + ",\"detail\":\"" + detail + "\"}";
                Test(binding, behavior, "POST", WebHttpBehavior3Tests.Endpoint + "/ThrowWebFaultExceptionOfT", "text/json", requestBody, statusCode, WebHttpBehavior3Tests.ApplicationJsonContentTypeWithCharset, "\"" + detail + "\"");
            }
        }

        [TestMethod]
        public void WebFaultExceptionOfJsonValue()
        {
            WebHttpBinding binding = new WebHttpBinding();
            WebHttpBehavior3 behavior = new WebHttpBehavior3();
            behavior.FaultExceptionEnabled = false;

            JsonValue[] details = new JsonValue[]
            {
                new JsonPrimitive(123),
                new JsonPrimitive("a string value"),
                new JsonArray { 123.456, null, false },
                new JsonObject { { "a", 123 }, { "b", new JsonArray("a", "b", "c") }, { "c", new JsonObject() } },
            };

            HttpStatusCode[] statusCodes = new HttpStatusCode[] { HttpStatusCode.RequestEntityTooLarge, HttpStatusCode.RequestedRangeNotSatisfiable, HttpStatusCode.NotImplemented };
            foreach (HttpStatusCode statusCode in statusCodes)
            {
                foreach (JsonValue detail in details)
                {
                    foreach (bool useExactJsonValueType in new bool[] { false, true })
                    {
                        string requestBody = detail.ToString();
                        string url = WebHttpBehavior3Tests.Endpoint + "/ThrowWebFaultExceptionOfJsonValue?statusCode=" + (int)statusCode + "&useExactJsonValueType=" + useExactJsonValueType.ToString().ToLowerInvariant();
                        Test(binding, behavior, "POST", url, "text/json", requestBody, statusCode, WebHttpBehavior3Tests.ApplicationJsonContentTypeWithCharset, requestBody);
                    }
                }
            }
        }

        [TestMethod]
        public void WebFaultExceptionOfJsonValueWithHttpResponseMessageProperty()
        {
            WebHttpBinding binding = new WebHttpBinding();
            WebHttpBehavior3 behavior = new WebHttpBehavior3();
            behavior.FaultExceptionEnabled = false;
            HttpStatusCode statusCode = HttpStatusCode.Conflict;

            Test(binding, behavior, "POST", WebHttpBehavior3Tests.Endpoint + "/ThrowWebFaultExceptionOfJsonValueChangingContentType?statusCode=" + (int)statusCode, "text/json", "123", statusCode, WebHttpBehavior3Tests.ApplicationJsonContentTypeWithCharset, "123");
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
            public JsonValue ThrowWebFaultException(int statusCode)
            {
                throw new WebFaultException((HttpStatusCode)statusCode);
            }

            public JsonValue ThrowWebFaultExceptionOfT(int statusCode, string detail)
            {
                throw new WebFaultException<string>(detail, (HttpStatusCode)statusCode);
            }

            public JsonValue ThrowWebFaultExceptionOfJsonValue(int statusCode, bool useExactJsonValueType, JsonValue detail)
            {
                HttpStatusCode httpStatusCode = (HttpStatusCode)statusCode;
                if (useExactJsonValueType)
                {
                    switch (detail.JsonType)
                    {
                        case JsonType.Array:
                            throw new WebFaultException<JsonArray>((JsonArray)detail, httpStatusCode);
                        case JsonType.Object:
                            throw new WebFaultException<JsonObject>((JsonObject)detail, httpStatusCode);
                        default:
                            throw new WebFaultException<JsonPrimitive>((JsonPrimitive)detail, httpStatusCode);
                    }
                }
                else
                {
                    throw new WebFaultException<JsonValue>(detail, httpStatusCode);
                }
            }

            public JsonValue ThrowWebFaultExceptionOfJsonValueChangingContentType(int statusCode, JsonValue detail)
            {
                WebOperationContext.Current.OutgoingResponse.ContentType = "text/plain";
                throw new WebFaultException<JsonValue>(detail, (HttpStatusCode)statusCode);
            }
        }
    }
}
