using System.ServiceModel.Channels;
using System.ServiceModel.Description;
using System.ServiceModel.Dispatcher;

namespace Microsoft.ApplicationServer.Http.Description
{
    public class InstanceProviderBehavior : IEndpointBehavior
    {
        private IInstanceProvider instanceProvider;

        public InstanceProviderBehavior(IInstanceProvider instanceProvider)
        {
            this.instanceProvider = instanceProvider;
        }

        public void AddBindingParameters(ServiceEndpoint endpoint, BindingParameterCollection bindingParameters)
        {
        }

        public void ApplyClientBehavior(ServiceEndpoint endpoint, ClientRuntime clientRuntime)
        {
        }

        public void ApplyDispatchBehavior(ServiceEndpoint endpoint, EndpointDispatcher endpointDispatcher)
        {
            endpointDispatcher.DispatchRuntime.InstanceProvider = this.instanceProvider;
        }

        public void Validate(ServiceEndpoint endpoint)
        {
        }
    }
}