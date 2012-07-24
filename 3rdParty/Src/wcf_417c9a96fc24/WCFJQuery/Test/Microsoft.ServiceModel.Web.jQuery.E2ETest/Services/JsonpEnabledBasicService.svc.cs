namespace Microsoft.ServiceModel.Web.E2ETest.Services
{
    using System;
    using System.Json;
    using System.Net;
    using System.ServiceModel;
    using System.ServiceModel.Activation;
    using System.ServiceModel.Description;
    using System.ServiceModel.Web;

    public class JsonpEnabledBasicService : IJsonpEnabledBasicService
    {
        public JsonValue EchoJsonValueGet()
        {
            WebOperationContext.Current.OutgoingResponse.Headers[HttpResponseHeader.CacheControl] = "no-cache";
            JsonValue queryParams = WebOperationContext.Current.IncomingRequest.GetQueryStringAsJsonObject();
            return queryParams;
        }
    }

    public class JsonpEnabledServiceHostFactory : ServiceHostFactory
    {
        protected override ServiceHost CreateServiceHost(Type serviceType, Uri[] baseAddresses)
        {
            return new JsonpEnabledBasicServiceHost(serviceType, baseAddresses);
        }
    }

    class JsonpEnabledBasicServiceHost : ServiceHost
    {
        public JsonpEnabledBasicServiceHost(Type serviceType, Uri[] baseAddresses)
            : base(serviceType, baseAddresses)
        {
        }

        protected override void InitializeRuntime()
        {
            WebHttpBinding binding = new WebHttpBinding();
            binding.CrossDomainScriptAccessEnabled = true;
            ServiceEndpoint endpoint = this.AddServiceEndpoint(typeof(IJsonpEnabledBasicService), binding, "");
            endpoint.Behaviors.Add(new WebHttpBehavior3());

            base.InitializeRuntime();
        }
    }
}
