namespace Microsoft.ApplicationServer.Http.Description
{
    using System;
    using System.ServiceModel.Description;

    internal class DelegateOperationConfiguration : IOperationConfiguration
    {
        private readonly Action<OperationDescription> configureOperation;

        public DelegateOperationConfiguration(Action<OperationDescription> configureOperation)
        {
            this.configureOperation = configureOperation;
        }

        public void Configure(OperationDescription operation)
        {
            this.configureOperation(operation);
        }
    }
}