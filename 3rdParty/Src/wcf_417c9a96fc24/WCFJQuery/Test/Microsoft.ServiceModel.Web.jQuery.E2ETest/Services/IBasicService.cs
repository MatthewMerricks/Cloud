namespace Microsoft.ServiceModel.Web.E2ETest.Services
{
    using System;
    using System.Json;
    using System.ServiceModel;
    using System.ServiceModel.Web;

    [ServiceContract]
    public interface IBasicService
    {
        [OperationContract]
        [WebGet(ResponseFormat = WebMessageFormat.Json)]
        JsonValue SumQueryStringParameters();

        [OperationContract]
        JsonValue EchoJsonValue(JsonValue input);

        [OperationContract]
        [WebGet(ResponseFormat = WebMessageFormat.Json)]
        JsonValue EchoJsonValueGet();

        [OperationContract]
        [WebInvoke(UriTemplate = "/ConvertIntoCLRType/{keyName}/{typeName}?format={toStringFormat}", ResponseFormat = WebMessageFormat.Json)]
        string ConvertIntoCLRType(string keyName, string typeName, string toStringFormat, JsonValue input);

        [OperationContract]
        [WebInvoke(UriTemplate = "/EchoDate/{keyName}", ResponseFormat = WebMessageFormat.Json)]
        JsonValue EchoDate(string keyName, JsonValue input);

        [OperationContract]
        [WebInvoke(UriTemplate = "/ValidateJsonValue/{keyName}/{validationMethod}", ResponseFormat = WebMessageFormat.Json)]
        JsonValue ValidateJsonValue(string keyName, string validationMethod, JsonValue input);
    }
}
