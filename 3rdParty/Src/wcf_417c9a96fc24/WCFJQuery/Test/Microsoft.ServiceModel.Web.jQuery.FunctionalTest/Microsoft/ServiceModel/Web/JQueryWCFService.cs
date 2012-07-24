namespace Microsoft.ServiceModel.Web.Test
{
    using System;
    using System.Json;
    using System.ServiceModel;
    using System.ServiceModel.Description;
    using System.ServiceModel.Web;
    using Microsoft.ServiceModel.Web;

    [ServiceContract]
    public interface IJQueryWCF
    {
        [OperationContract]
        [WebInvoke(Method = "POST", UriTemplate = "/JQuery/{cont}/{blob}", ResponseFormat = WebMessageFormat.Json, BodyStyle = WebMessageBodyStyle.Bare)]
        JsonValue PutFormToJsonValue(string cont, string blob, JsonValue jvContent);

        [OperationContract]
        [WebGet(UriTemplate = "/JQueryGet/{cont}/{blob}?Address={address}", ResponseFormat = WebMessageFormat.Json)]
        JsonValue GetFormToJsonValue(string cont, string blob, string address);

        [OperationContract]
        [WebInvoke(BodyStyle = WebMessageBodyStyle.WrappedRequest, ResponseFormat = WebMessageFormat.Xml)]
        int AddXml(int x, int y);

        [OperationContract]
        [WebInvoke(BodyStyle = WebMessageBodyStyle.WrappedRequest, ResponseFormat = WebMessageFormat.Json)]
        int AddJson(int x, int y);

        [WebInvoke(BodyStyle = WebMessageBodyStyle.WrappedRequest)]
        int AddJsonOrXml(int x, int y);

        [OperationContract]
        [WebInvoke(BodyStyle = WebMessageBodyStyle.Bare)]
        JsonValue Echo(JsonValue input);

        [OperationContract, WebGet]
        JsonValue EchoGet();

        [OperationContract, WebGet]
        JsonValue GetSettingHttpHeaders();

        [OperationContract, WebInvoke]
        JsonValue PostSettingHttpHeaders(JsonValue input);

        [OperationContract, WebInvoke(UriTemplate = "/GetKeyValue/{keyName}")]
        JsonValue GetKeyValue(JsonValue input, string keyName);

        [WebInvoke(UriTemplate = "/GetArrayMember/{memberName}")]
        JsonArray GetArrayMember(string memberName, JsonValue input);

        [WebInvoke(UriTemplate = "/GetObjectMember/{memberName}")]
        JsonObject GetObjectMember(string memberName, JsonValue input);

        [WebInvoke(UriTemplate = "/GetPrimitiveMember/{memberName}")]
        JsonPrimitive GetPrimitiveMember(string memberName, JsonValue input);
    }

    [ServiceBehavior(IncludeExceptionDetailInFaults = true)]
    public class JQueryWCFService : IJQueryWCF
    {
        public JsonValue PutFormToJsonValue(string cont, string blob, JsonValue jvContent)
        {
            if (jvContent != null)
            {
                jvContent["customer"]["Name"] = "Yavor";
                return jvContent["customer"];
            }

            return jvContent;
        }

        public JsonValue GetFormToJsonValue(string cont, string blob, string address)
        {
            JsonValue jvContent = WebOperationContext.Current.IncomingRequest.GetQueryStringAsJsonObject();
            jvContent["customer"]["Name"] = "Yavor";
            jvContent["customer"]["Address"] = address;
            return jvContent["customer"];
        }

        public int AddXml(int x, int y)
        {
            return x + y;
        }

        public int AddJson(int x, int y)
        {
            return x + y;
        }

        public int AddJsonOrXml(int x, int y)
        {
            return x + y;
        }

        public JsonValue Echo(JsonValue input)
        {
            return input;
        }

        public JsonValue EchoGet()
        {
            return WebOperationContext.Current.IncomingRequest.GetQueryStringAsJsonObject();
        }

        public JsonValue GetSettingHttpHeaders()
        {
            JsonValue queryParams = WebOperationContext.Current.IncomingRequest.GetQueryStringAsJsonObject();
            if (queryParams.JsonType == JsonType.Object)
            {
                this.SetHttpHeadersInResponse((JsonObject)queryParams);
            }

            return queryParams;
        }

        public JsonValue PostSettingHttpHeaders(JsonValue input)
        {
            if (input.JsonType == JsonType.Object)
            {
                this.SetHttpHeadersInResponse((JsonObject)input);
            }

            return input;
        }

        public JsonValue GetKeyValue(JsonValue input, string keyName)
        {
            JsonObject obj = input as JsonObject;
            if (obj != null && obj.ContainsKey(keyName))
            {
                return obj[keyName];
            }
            else
            {
                return null;
            }
        }

        public JsonArray GetArrayMember(string memberName, JsonValue input)
        {
            return input[memberName] as JsonArray;
        }

        public JsonObject GetObjectMember(string memberName, JsonValue input)
        {
            return input[memberName] as JsonObject;
        }

        public JsonPrimitive GetPrimitiveMember(string memberName, JsonValue input)
        {
            return input[memberName] as JsonPrimitive;
        }

        void SetHttpHeadersInResponse(JsonObject headers)
        {
            foreach (string name in headers.Keys)
            {
                WebOperationContext.Current.OutgoingResponse.Headers.Add(name, (string)headers[name]);
            }
        }
    }
}
