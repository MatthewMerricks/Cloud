namespace Microsoft.ServiceModel.Web.UnitTests
{
    using System.ComponentModel.DataAnnotations;
    using System.IO;
    using System.Json;
    using System.Net;
    using System.ServiceModel;
    using System.ServiceModel.Web;
    using System.Xml.Linq;

    [ServiceContract]
    public interface ITestService
    {
        [OperationContract]
        JsonObject EchoPost(JsonObject input);

        [WebInvoke(UriTemplate = "/{id}")]
        JsonValue GetField(string id, JsonValue input);

        [WebGet]
        JsonValue EchoGet();

        [WebGet]
        JsonValue EchoNull();

        [WebInvoke(Method = "HEAD")]
        void EchoHead();

        [WebInvoke(UriTemplate = "/EchoWithFormat?format={format}")]
        JsonValue EchoWithFormat(string format, JsonValue input);

        [WebInvoke(UriTemplate = "/ThrowWebFaultExceptionOfJsonValueResponseFormatJson?statusCode={statusCode}", ResponseFormat = WebMessageFormat.Json)]
        int ThrowWebFaultExceptionOfJsonValueResponseFormatJson(int statusCode, JsonValue detail);

        [WebInvoke(UriTemplate = "/ThrowWebFaultExceptionOfJsonValueResponseFormatXml?statusCode={statusCode}", ResponseFormat = WebMessageFormat.Xml)]
        int ThrowWebFaultExceptionOfJsonValueResponseFormatXml(int statusCode, JsonValue detail);

        [WebGet(ResponseFormat = WebMessageFormat.Json)]
        int ThrowWebFaultExceptionResponseFormatJson(int statusCode, bool throwWebFaultOfXElement);

        [WebGet(ResponseFormat = WebMessageFormat.Xml)]
        int ThrowWebFaultExceptionResponseFormatXml(int statusCode, bool throwWebFaultOfXElement);

        [WebInvoke(ResponseFormat = WebMessageFormat.Json)]
        Customer ThrowValidation(Customer customer);
    }

    [ServiceContract]
    public interface ITestService35
    {
        [OperationContract]
        Customer EchoPost(Customer input);

        [WebInvoke(UriTemplate = "/{id}")]
        Customer GetField(string id, Customer input);

        [WebGet]
        Customer EchoNull();
    }

    public class TestService : ITestService
    {
        public const string BaseAddress = "http://localhost:8000/Service";

        public JsonObject EchoPost(JsonObject input)
        {
            return input;
        }

        public JsonValue GetField(string id, JsonValue input)
        {
            JsonObject jo = input as JsonObject;
            if (jo != null && jo.ContainsKey(id))
            {
                return jo[id];
            }
            else
            {
                return null;
            }
        }

        public JsonValue EchoGet()
        {
            return WebOperationContext.Current.IncomingRequest.GetQueryStringAsJsonObject();
        }

        public JsonValue EchoNull()
        {
            return null;
        }

        public void EchoHead()
        {
            JsonValue result = this.EchoGet();
            using (MemoryStream ms = new MemoryStream())
            {
                result.Save(ms);
                WebOperationContext.Current.OutgoingResponse.ContentType = "application/json; charset=utf-8";
                WebOperationContext.Current.OutgoingResponse.ContentLength = ms.Position;
            }
        }

        public JsonValue EchoWithFormat(string format, JsonValue input)
        {
            if (format == "json")
            {
                WebOperationContext.Current.OutgoingResponse.Format = WebMessageFormat.Json;
            }
            else if (format == "xml")
            {
                WebOperationContext.Current.OutgoingResponse.Format = WebMessageFormat.Xml;
            }

            return input;
        }

        public int ThrowWebFaultExceptionOfJsonValueResponseFormatJson(int statusCode, JsonValue detail)
        {
            JsonObject jo = detail as JsonObject;

            if (jo != null && jo.Count == 0)
            {
                detail = null;
            }

            throw new WebFaultException<JsonValue>(detail, (HttpStatusCode)statusCode);
        }

        public int ThrowWebFaultExceptionOfJsonValueResponseFormatXml(int statusCode, JsonValue detail)
        {
            return this.ThrowWebFaultExceptionOfJsonValueResponseFormatJson(statusCode, detail);
        }

        public int ThrowWebFaultExceptionResponseFormatJson(int statusCode, bool throwWebFaultOfXElement)
        {
            HttpStatusCode httpStatusCode = (HttpStatusCode)statusCode;
            if (throwWebFaultOfXElement)
            {
                throw new WebFaultException<XElement>(XElement.Parse("<statusCode>" + statusCode + "</statusCode>"), httpStatusCode);
            }
            else
            {
                throw new WebFaultException(httpStatusCode);
            }
        }

        public int ThrowWebFaultExceptionResponseFormatXml(int statusCode, bool throwWebFaultOfXElement)
        {
            return this.ThrowWebFaultExceptionResponseFormatJson(statusCode, throwWebFaultOfXElement);
        }

        public Customer ThrowValidation(Customer customer)
        {
            return customer;
        }
    }

    public class TestService35 : ITestService35
    {
        public const string BaseAddress = "http://localhost:8000/Service";

        public Customer EchoPost(Customer input)
        {
            return input;
        }

        public Customer GetField(string id, Customer input)
        {
            return input;
        }

        public Customer EchoNull()
        {
            return null;
        }
    }

    public class Customer
    {
        [Required]
        [Range(18, 20)]
        public int Age { get; set; }
    }
}