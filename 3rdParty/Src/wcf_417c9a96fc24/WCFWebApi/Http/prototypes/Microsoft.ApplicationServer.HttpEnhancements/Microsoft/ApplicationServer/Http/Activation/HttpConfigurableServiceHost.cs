// <copyright>
//   Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>

using System.Collections.ObjectModel;
using System.ServiceModel.Dispatcher;

namespace Microsoft.ApplicationServer.Http.Activation
{
    using System;
    using System.Collections.Generic;
    using System.ServiceModel;
    using System.ServiceModel.Activation;
    using System.ServiceModel.Description;
    using System.ServiceModel.Web;
    using Microsoft.ApplicationServer.Http;
    using Microsoft.ApplicationServer.Http.Description;

    public class HttpConfigurableServiceHost : HttpServiceHost
    {
        internal HttpHostConfiguration configuration;
        internal Type serviceType;

        public HttpConfigurableServiceHost(object singletonInstance, IHttpHostConfigurationBuilder builder, params Uri[] baseAddresses)
            : base(singletonInstance, baseAddresses)
        {
            this.serviceType = singletonInstance.GetType();
            this.SetAspNetCompatabilityRequirements();
            Configure(builder.Configuration);
        }

        public HttpConfigurableServiceHost(Type serviceType, IHttpHostConfigurationBuilder builder, params Uri[] baseAddresses)
            : base(serviceType, baseAddresses)
        {
            this.serviceType = serviceType;
            this.SetAspNetCompatabilityRequirements();
            if (builder == null)
                this.Configure(new HttpHostConfiguration());
            else 
                Configure(builder.Configuration);
        }

        private void SetAspNetCompatabilityRequirements()
        {
            this.Description.Behaviors.Remove<AspNetCompatibilityRequirementsAttribute>();
            this.Description.Behaviors.Add(
                new AspNetCompatibilityRequirementsAttribute { RequirementsMode = AspNetCompatibilityRequirementsMode.Allowed });
        }

        private void Configure(HttpHostConfiguration configuration)
        {
            this.configuration = configuration;
            this.OperationHandlerFactory = configuration.OperationHandlerFactory;
            this.MessageHandlerFactory = configuration.MessageHandlerFactory;
            AddDefaultEndpoints();
        }

        public override void AddServiceEndpoint(ServiceEndpoint endpoint)
        {
            var httpEndpoint = (HttpEndpoint)endpoint;
            if (configuration != null)
            {
                httpEndpoint.OperationHandlerFactory = configuration.OperationHandlerFactory;
                httpEndpoint.MessageHandlerFactory = configuration.MessageHandlerFactory;
                if (this.configuration.ErrorHandler != null)
                {
                    var behavior = endpoint.Behaviors.Remove<HttpBehavior>();
                    endpoint.Behaviors.Add(new HttpBehaviorWithErrorHandler(this.configuration.ErrorHandler)
                                               {OperationHandlerFactory = this.configuration.OperationHandlerFactory});
                }
                if (this.configuration.InstanceFactory != null)
                {
                    var behavior =
                        new InstanceProviderBehavior(new ResourceFactoryProvider(this.serviceType,
                                                                                 this.configuration.InstanceFactory));
                    endpoint.Behaviors.Add(behavior);
                }
            }
            base.AddServiceEndpoint(endpoint);
        }
    }
}
