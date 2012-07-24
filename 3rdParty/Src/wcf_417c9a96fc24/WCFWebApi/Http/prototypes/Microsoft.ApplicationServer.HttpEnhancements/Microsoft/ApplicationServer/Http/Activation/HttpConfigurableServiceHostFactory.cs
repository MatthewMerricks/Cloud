// <copyright>
//   Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>

using Microsoft.ApplicationServer.Http.Description;

namespace Microsoft.ApplicationServer.Http.Activation
{
    using System;
    using System.Diagnostics.CodeAnalysis;
    using System.ServiceModel.Activation;

    public class HttpConfigurableServiceHostFactory : ServiceHostFactory , IConfigurableServiceHostFactory
    {
        public HttpConfigurableServiceHostFactory()
        {
        }

        public HttpConfigurableServiceHostFactory(IHttpHostConfigurationBuilder builder)
        {
            this.Builder = builder;
        }

        protected override System.ServiceModel.ServiceHost CreateServiceHost(Type serviceType, Uri[] baseAddresses)
        {
            var host = new HttpConfigurableServiceHost(serviceType, this.Builder, baseAddresses);
            return host;
        }

        public IHttpHostConfigurationBuilder Builder { get; set; }
    }
}
