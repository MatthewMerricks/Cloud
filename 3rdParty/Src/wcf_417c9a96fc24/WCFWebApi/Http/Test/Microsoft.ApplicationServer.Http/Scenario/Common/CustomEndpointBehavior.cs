// <copyright>
//   Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>

namespace Microsoft.ApplicationServer.Http.CIT.Scenario.Common
{
    using System;
    using System.Linq;
    using System.ServiceModel.Channels;
    using System.ServiceModel.Description;
    using System.ServiceModel.Dispatcher;
    using Microsoft.ApplicationServer.Http.Description;

    internal class CustomEndpointBehavior : IEndpointBehavior
    {
        public void AddBindingParameters(ServiceEndpoint endpoint, BindingParameterCollection bindingParameters)
        {
            // do nothing;
        }

        public void ApplyClientBehavior(ServiceEndpoint endpoint, ClientRuntime clientRuntime)
        {
            // do nothing
        }

        public void ApplyDispatchBehavior(ServiceEndpoint endpoint, EndpointDispatcher endpointDispatcher)
        {
            if (endpoint == null)
            {
                throw new ArgumentNullException("endpoint");
            }

            if (endpointDispatcher == null)
            {
                throw new ArgumentNullException("endpointDispatcher");
            }

            // Set permisive address and contract filters
            endpointDispatcher.AddressFilter = new PrefixEndpointAddressMessageFilter(endpoint.Address);
            endpointDispatcher.ContractFilter = new MatchAllMessageFilter();

            var httpOperations = endpoint.Contract.Operations.Select( od => od.ToHttpOperationDescription());

            // Set the OperationSelector        
            endpointDispatcher.DispatchRuntime.OperationSelector =
                new CustomOperationSelector(endpoint.Address.Uri, httpOperations);

            // Set the OperationSelector        
            endpointDispatcher.DispatchRuntime.MessageInspectors.Add(
               new CustomMessageInspector());

            // Set the ErrorHandler
            endpointDispatcher.ChannelDispatcher.ErrorHandlers.Clear();
            endpointDispatcher.ChannelDispatcher.ErrorHandlers.Add(new CustomErrorHandler());

            // Set the Formatters
            foreach (HttpOperationDescription httpOperation in httpOperations)
            {
                var dispatchOperation = endpointDispatcher.DispatchRuntime.Operations[httpOperation.Name];
                dispatchOperation.Formatter = new CustomMessageFormatter(httpOperation);
            }
        }

        public void Validate(ServiceEndpoint endpoint)
        {
            // do nothing
        }
    }
}
