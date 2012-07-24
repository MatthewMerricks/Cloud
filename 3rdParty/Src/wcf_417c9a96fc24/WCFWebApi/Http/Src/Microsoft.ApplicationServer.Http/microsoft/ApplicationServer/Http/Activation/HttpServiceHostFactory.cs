// <copyright>
//   Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>

namespace Microsoft.ApplicationServer.Http.Activation
{
    using System;
    using System.ComponentModel;
    using System.ServiceModel;
    using System.ServiceModel.Activation;
    using Microsoft.ApplicationServer.Http.Channels;
    using Microsoft.ApplicationServer.Http.Description;

    /// <summary>
    /// <see cref="ServiceHostFactory"/> derived class that can create <see cref="HttpServiceHost"/> instances.
    /// </summary>
    public class HttpServiceHostFactory : ServiceHostFactory
    {
        /// <summary>
        /// Gets or sets the default <see cref="HttpMessageHandlerFactory"/> to use for
        /// the <see cref="HttpServiceHost"/>instances created by the <see cref="HttpServiceHostFactory"/>.
        /// </summary>
        [DefaultValue(null)]
        public HttpMessageHandlerFactory MessageHandlerFactory { get; set; }

        /// <summary>
        /// Gets or sets the default <see cref="HttpOperationHandlerFactory"/> to use for
        /// the <see cref="HttpServiceHost"/>instances created by the <see cref="HttpServiceHostFactory"/>.
        /// </summary>
        [DefaultValue(null)]
        public HttpOperationHandlerFactory OperationHandlerFactory { get; set; }

        /// <summary>
        /// Creates a new <see cref="HttpServiceHost"/> instance.
        /// </summary>
        /// <param name="serviceType">Specifies the type of service to host.</param>
        /// <param name="baseAddresses">The base addresses for the service hosted.</param>
        /// <returns>A new <see cref="HttpServiceHost"/> instance.</returns>
        protected override ServiceHost CreateServiceHost(Type serviceType, Uri[] baseAddresses)
        {
            HttpServiceHost host = new HttpServiceHost(serviceType, baseAddresses);
            host.MessageHandlerFactory = this.MessageHandlerFactory;
            host.OperationHandlerFactory = this.OperationHandlerFactory;

            return host;
        }
    }
}
