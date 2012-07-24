// <copyright>
//   Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>

namespace Microsoft.ApplicationServer.Http.CIT.Scenario.Common
{
    using System.ServiceModel.Channels;
    using System.ServiceModel.Description;
    using System.ServiceModel.Dispatcher;
    using Microsoft.ApplicationServer.Common;
    using System;

    /// <summary>
    /// This behavior explicitly adds an instance of the <see cref="ServiceDebugBehavior"/> and the <see cref="DispatcherSynchronizationBehavior"/> 
    /// as binding parameters during <see cref="IEndpointBehavior.AddBindingParameters"/>. This makes these behaviors 
    /// visible to the <see cref="HttpMessageHandlerChannel"/> which is necessary for it to operate correctly.
    /// </summary>
    internal class TestHttpBindingParameterBehavior : IEndpointBehavior
    {
        private ServiceDebugBehavior serviceDebugBehavior;
        private DispatcherSynchronizationBehavior dispatcherSynchronizationBehavior;

        /// <summary>
        /// Initializes a new instance of the <see cref="HttpBindingParameterBehavior"/> class.
        /// </summary>
        /// <param name="serviceDebugBehavior">Optional service debug behavior (can be null).</param>
        /// <param name="dispatcherSynchronizationBehavior">Optional dispatcher synchronization behavior (can be null).</param>
        public TestHttpBindingParameterBehavior(ServiceDebugBehavior serviceDebugBehavior, DispatcherSynchronizationBehavior dispatcherSynchronizationBehavior)
        {
            this.serviceDebugBehavior = serviceDebugBehavior;
            this.dispatcherSynchronizationBehavior = dispatcherSynchronizationBehavior;
        }

        /// <summary>
        /// Validates the endpoint meets the criteria required to enable this behavior.
        /// </summary>
        /// <param name="serviceEndpoint">The endpoint to validate.</param>
        void IEndpointBehavior.Validate(ServiceEndpoint serviceEndpoint)
        {
        }

        /// <summary>
        /// Adds the binding parameters.
        /// </summary>
        /// <param name="serviceEndpoint">The service endpoint.</param>
        /// <param name="parameters">The parameters.</param>
        void IEndpointBehavior.AddBindingParameters(ServiceEndpoint serviceEndpoint, BindingParameterCollection parameters)
        {
            if (parameters == null)
            {
                throw new ArgumentNullException("parameters");
            }

            ServiceDebugBehavior debugBehavior = parameters.Find<ServiceDebugBehavior>();
            if (debugBehavior == null && this.serviceDebugBehavior != null)
            {
                parameters.Add(this.serviceDebugBehavior);
            }

            DispatcherSynchronizationBehavior synchronizationBehavior = parameters.Find<DispatcherSynchronizationBehavior>();
            if (synchronizationBehavior == null && this.dispatcherSynchronizationBehavior != null)
            {
                parameters.Add(this.dispatcherSynchronizationBehavior);
            }
        }

        /// <summary>
        /// Applies the dispatch behavior.
        /// </summary>
        /// <param name="serviceEndpoint">The service endpoint.</param>
        /// <param name="endpointDispatcher">The endpoint dispatcher.</param>
        void IEndpointBehavior.ApplyDispatchBehavior(ServiceEndpoint serviceEndpoint, EndpointDispatcher endpointDispatcher)
        {
        }

        /// <summary>
        /// Applies the client behavior.
        /// </summary>
        /// <param name="serviceEndpoint">The service endpoint.</param>
        /// <param name="behavior">The behavior.</param>
        void IEndpointBehavior.ApplyClientBehavior(ServiceEndpoint serviceEndpoint, ClientRuntime behavior)
        {
        }
    }
}
