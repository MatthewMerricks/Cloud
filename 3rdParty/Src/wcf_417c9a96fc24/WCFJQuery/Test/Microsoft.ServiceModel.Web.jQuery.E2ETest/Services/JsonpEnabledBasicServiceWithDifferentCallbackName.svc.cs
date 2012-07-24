namespace Microsoft.ServiceModel.Web.E2ETest.Services
{
    using System.Json;
    using System.Net;
    using System.ServiceModel.Web;

    [JavascriptCallbackBehavior(UrlParameterName = "newCallback")]
    public class JsonpEnabledBasicServiceWithDifferentCallbackName : IJsonpEnabledBasicService
    {
        public JsonValue EchoJsonValueGet()
        {
            WebOperationContext.Current.OutgoingResponse.Headers[HttpResponseHeader.CacheControl] = "no-cache";
            JsonValue queryParams = WebOperationContext.Current.IncomingRequest.GetQueryStringAsJsonObject();
            return queryParams;
        }
    }
}
