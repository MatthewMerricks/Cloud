// <copyright>
//   Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>

namespace Microsoft.ApplicationServer.Http
{
    using System;
    using System.Collections.Generic;
    using System.Collections.ObjectModel;
    using System.ComponentModel;
    using System.Linq;
    using System.ServiceModel;
    using System.ServiceModel.Description;
    using Microsoft.ApplicationServer.Common;
    using Microsoft.ApplicationServer.Http.Channels;
    using Microsoft.ApplicationServer.Http.Description;

    /// <summary>
    /// Class that provides a <see cref="ServiceHost"/> for the <see cref="HttpBinding"/> binding.
    /// </summary>
    public class HttpServiceHost : ServiceHost
    {
        private static readonly string httpServiceHostTypeName = typeof(HttpServiceHost).Name;
        private static readonly ReadOnlyCollection<ServiceEndpoint> emptyReadOnlyCollectionOfServiceEndpoints = new ReadOnlyCollection<ServiceEndpoint>(new List<ServiceEndpoint>());

        /// <summary>
        /// Initializes a new instance of the <see cref="HttpServiceHost"/> class.
        /// </summary>
        public HttpServiceHost()
            : base()
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="HttpServiceHost"/> class.
        /// </summary>
        /// <param name="singletonInstance">The instance of the hosted service.</param>
        /// <param name="baseAddresses">The base addresses for the hosted service.</param>
        public HttpServiceHost(object singletonInstance, params Uri[] baseAddresses)
            : base(singletonInstance, ValidateBaseAddresses(baseAddresses))
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="HttpServiceHost"/> class.
        /// </summary>
        /// <param name="serviceType">The type of hosted service.</param>
        /// <param name="baseAddresses">The base addresses for the hosted service.</param>
        public HttpServiceHost(Type serviceType, params Uri[] baseAddresses) 
            : base(serviceType, ValidateBaseAddresses(baseAddresses))
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="HttpServiceHost"/> class.
        /// </summary>
        /// <param name="singletonInstance">The instance of the hosted service.</param>
        /// <param name="baseAddresses">The base addresses for the hosted service.</param>
        public HttpServiceHost(object singletonInstance, params string[] baseAddresses)
            : base(singletonInstance, CreateUriBaseAddresses(baseAddresses))
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="HttpServiceHost"/> class.
        /// </summary>
        /// <param name="serviceType">The type of hosted service.</param>
        /// <param name="baseAddresses">The base addresses for the hosted service.</param>
        public HttpServiceHost(Type serviceType, params string[] baseAddresses)
            : base(serviceType, CreateUriBaseAddresses(baseAddresses))
        {
        }

        /// <summary>
        /// Gets or sets the default <see cref="HttpMessageHandlerFactory"/> to use for
        /// the <see cref="HttpEndpoint"/> instances created by the <see cref="HttpServiceHost"/>.
        /// </summary>
        [DefaultValue(null)]
        public HttpMessageHandlerFactory MessageHandlerFactory { get; set; }

        /// <summary>
        /// Gets or sets the default <see cref="HttpOperationHandlerFactory"/> to use for
        /// the <see cref="HttpEndpoint"/> instances created by the <see cref="HttpServiceHost"/>.
        /// </summary>
        [DefaultValue(null)]
        public HttpOperationHandlerFactory OperationHandlerFactory { get; set; }

        /// <summary>
        /// Adds service endpoints for all base addresses in each contract found in the service host
        /// with the default binding.
        /// </summary>
        /// <returns>A read-only collection of default endpoints.</returns>
        public override ReadOnlyCollection<ServiceEndpoint> AddDefaultEndpoints()
        {
            if (this.Description != null &&  
                this.BaseAddresses.Count > 0 &&
                (this.Description.Endpoints == null || this.Description.Endpoints.Count == 0))
            {
                List<ServiceEndpoint> defaultEndpoints = new List<ServiceEndpoint>();

                foreach (Uri baseAddress in this.BaseAddresses)
                {
                    if (Object.ReferenceEquals(baseAddress.Scheme, Uri.UriSchemeHttp) || Object.ReferenceEquals(baseAddress.Scheme, Uri.UriSchemeHttps))
                    {
                        ServiceEndpoint defaultEndpoint = this.GenerateDefaultServiceEndpoint(baseAddress);
                        defaultEndpoints.Add(defaultEndpoint);
                        this.AddServiceEndpoint(defaultEndpoint);
                    }
                }

                return new ReadOnlyCollection<ServiceEndpoint>(defaultEndpoints);
            }

            return emptyReadOnlyCollectionOfServiceEndpoints;
        }

        /// <summary>
        /// Invoked during the transition of a communication object into the opening state.
        /// </summary>
        protected override void OnOpening()
        {
            if (this.Description == null)
            {
                return;
            }

            DisableServiceDebugAndMetadataBehaviors(this.Description);
            ServiceDebugBehavior debugBehavior = this.Description.Behaviors.Find<ServiceDebugBehavior>();
            foreach (ServiceEndpoint serviceEndpoint in this.Description.Endpoints)
            {
                if (serviceEndpoint.Binding != null)
                {
                    if (serviceEndpoint.Binding.CreateBindingElements().Find<HttpMessageHandlerBindingElement>() != null)
                    {
                        DispatcherSynchronizationBehavior synchronizationBehavior = serviceEndpoint.Behaviors.Find<DispatcherSynchronizationBehavior>();
                        if (synchronizationBehavior == null)
                        {
                            synchronizationBehavior = new DispatcherSynchronizationBehavior() { AsynchronousSendEnabled = true };
                            serviceEndpoint.Behaviors.Add(synchronizationBehavior);
                        }

                        if (serviceEndpoint.Behaviors.Find<HttpBindingParameterBehavior>() == null)
                        {
                            serviceEndpoint.Behaviors.Add(new HttpBindingParameterBehavior(debugBehavior, synchronizationBehavior));
                        }
                    }

                    if (serviceEndpoint.Binding.CreateBindingElements().Find<HttpMessageEncodingBindingElement>() != null)
                    {
                        if (serviceEndpoint.Behaviors.Find<HttpBehavior>() == null)
                        {
                            if (serviceEndpoint.Behaviors.Find<HttpBehavior>() == null)
                            {
                                serviceEndpoint.Behaviors.Add(new HttpBehavior());
                            }
                        }
                    }
                }
            }

            base.OnOpening();
        }

        private static Uri[] CreateUriBaseAddresses(string[] baseAddresses)
        {
            if (baseAddresses == null)
            {
                throw Fx.Exception.ArgumentNull("baseAddresses");
            }

            List<Uri> uris = new List<Uri>();

            foreach (string baseAddress in baseAddresses)
            {
                if (!string.IsNullOrWhiteSpace(baseAddress))
                {
                    uris.Add(new Uri(baseAddress));
                }
            }

            return uris.ToArray();
        }

        private static Uri[] ValidateBaseAddresses(Uri[] baseAddresses)
        {
            if (baseAddresses == null)
            {
                throw Fx.Exception.ArgumentNull("baseAddresses");
            }

            List<Uri> uris = new List<Uri>();

            foreach (Uri baseAddress in baseAddresses)
            {
                if (baseAddress != null)
                {
                    uris.Add(baseAddress);
                }
            }

            return uris.ToArray();
        }

        private static void DisableServiceDebugAndMetadataBehaviors(ServiceDescription serviceDescription)
        {
            Fx.Assert(serviceDescription != null, "The 'serviceDescription' parameter should not be null.");

            ServiceDebugBehavior sdb = serviceDescription.Behaviors.Find<ServiceDebugBehavior>();
            if (sdb != null)
            {
                sdb.HttpHelpPageEnabled = false;
                sdb.HttpsHelpPageEnabled = false;
            }

            ServiceMetadataBehavior smb = serviceDescription.Behaviors.Find<ServiceMetadataBehavior>();
            if (smb != null)
            {
                smb.HttpGetEnabled = false;
                smb.HttpsGetEnabled = false;
            }
        }

        private ServiceEndpoint GenerateDefaultServiceEndpoint(Uri baseAddress)
        {
            if (this.ImplementedContracts.Count != 1)
            {
                throw new InvalidOperationException(SR.DefaultEndpointsServiceWithMultipleContracts(this.Description.Name, httpServiceHostTypeName));
            }

            ContractDescription contractDescription = this.ImplementedContracts.Values.First();

            HttpEndpoint endpoint = new HttpEndpoint(contractDescription, new EndpointAddress(baseAddress));
            endpoint.MessageHandlerFactory = this.MessageHandlerFactory;
            endpoint.OperationHandlerFactory = this.OperationHandlerFactory;

            return endpoint;
        }
    }
}
