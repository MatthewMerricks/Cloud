namespace Microsoft.ServiceModel.Web.E2ETest.Services
{
    using System.Json;
    using System.ServiceModel;
    using System.ServiceModel.Web;

    [ServiceContract]
    public interface IJsonpEnabledBasicService
    {
        [WebGet(ResponseFormat = WebMessageFormat.Json)]
        JsonValue EchoJsonValueGet();
    }
}
