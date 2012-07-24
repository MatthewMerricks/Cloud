namespace Microsoft.ApplicationServer.Http.Description
{
    using System;
    using System.ServiceModel.Description;

    internal class DelegateEndpointConfiguration : IEndpointConfiguration
    {
        private readonly Action<ServiceEndpoint> configureEndpoint;

        public DelegateEndpointConfiguration(Action<ServiceEndpoint> configureEndpoint)
        {
            this.configureEndpoint = configureEndpoint;
        }

        public void Configure(ServiceEndpoint endpoint)
        {
            this.configureEndpoint(endpoint);
        }
    }
}