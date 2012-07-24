// <copyright file="WebServiceHostFactory3.cs" company="Microsoft Corporation">
//   Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>

namespace Microsoft.ServiceModel.Activation
{
    using System;
    using System.ServiceModel;
    using System.ServiceModel.Activation;
    using Microsoft.ServiceModel.Web;

    /// <summary>
    /// A factory that provides instances of <see cref="Microsoft.ServiceModel.Web.WebServiceHost3"/> in
    /// managed hosting environments where the host instance is created dynamically in response to incoming
    /// messages.
    /// </summary>
    public class WebServiceHostFactory3 : ServiceHostFactory
    {
        /// <summary>
        /// Creates an instance of the <see cref="Microsoft.ServiceModel.Web.WebServiceHost3"/> class for
        /// the specified service with the specified base addresses.
        /// </summary>
        /// <param name="serviceType">The type of service to be hosted.</param>
        /// <param name="baseAddresses">An array of base addresses for the service.</param>
        /// <returns>An instance of the <see cref="Microsoft.ServiceModel.Web.WebServiceHost3"/> class.</returns>
        protected override ServiceHost CreateServiceHost(Type serviceType, Uri[] baseAddresses)
        {
            return new WebServiceHost3(serviceType, baseAddresses);
        }
    }
}
