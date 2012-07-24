namespace Microsoft.ApplicationServer.Http.Description
{
    using System;
    using System.ServiceModel.Description;

    internal class DelegateServiceConfiguration : IServiceConfiguration
    {
        private readonly Action<ServiceDescription> configureService;

        public DelegateServiceConfiguration(Action<ServiceDescription> configureService)
        {
            this.configureService = configureService;
        }

        public void Configure(ServiceDescription service)
        {
            this.configureService(service);
        }
    }
}