namespace Microsoft.ApplicationServer.Http.Description
{
    using System;
    using System.ServiceModel.Description;

    internal class DelegateContractConfiguration : IContractConfiguration
    {
        private readonly Action<ContractDescription> configureContract;

        public DelegateContractConfiguration(Action<ContractDescription> configureContract)
        {
            this.configureContract = configureContract;
        }

        public void Configure(ContractDescription contract)
        {
            this.configureContract(contract);
        }
    }
}