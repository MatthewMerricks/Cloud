// <copyright>
//   Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>

namespace Microsoft.ApplicationServer.Http.CIT.Scenario.Common
{
    using System;
    using System.ServiceModel;
    using System.ServiceModel.Description;

    internal class CustomServiceHost : ServiceHost
    {
        public CustomServiceHost(Type serviceType, params Uri[] baseAddresses)
            : base(serviceType, baseAddresses)
        {
            if (serviceType == null)
            {
                throw new ArgumentNullException("serviceType");
            }

            if (baseAddresses == null)
            {
                throw new ArgumentNullException("baseAddresses");
            }

            ContractDescription contract = ContractDescription.GetContract(serviceType);

            foreach (Uri baseAddress in baseAddresses)
            {
                ServiceEndpoint endpoint = new ServiceEndpoint(contract, new HttpBinding(), new EndpointAddress(baseAddress));
                endpoint.Behaviors.Add(new CustomEndpointBehavior());
                this.AddServiceEndpoint(endpoint);
            }
        }
    }
}
