// <copyright file="WebServiceHost3.cs" company="Microsoft Corporation">
//   Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>

namespace Microsoft.ServiceModel.Web
{
    using System;
    using System.ServiceModel.Description;
    using System.ServiceModel.Web;

    /// <summary>
    /// A <see cref="System.ServiceModel.Web.WebServiceHost"/> derived class which adds
    /// <see cref="System.Json.JsonValue"/> and validation support to the REST programming model.
    /// </summary>
    public class WebServiceHost3 : WebServiceHost
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="Microsoft.ServiceModel.Web.WebServiceHost3"/> class with the specified service type and base address.
        /// </summary>
        /// <param name="serviceType">The service type.</param>
        /// <param name="baseAddresses">The base addresses of the service.</param>
        public WebServiceHost3(Type serviceType, params Uri[] baseAddresses)
            : base(serviceType, baseAddresses)
        {
        }

        /// <summary>
        /// Called when the <see cref="Microsoft.ServiceModel.Web.WebServiceHost3"/> opens.
        /// </summary>
        /// <remarks>When the method is called, it replaces, for all endpoints in this host, the
        /// <see cref="System.ServiceModel.Description.WebHttpBehavior"/> with the
        /// <see cref="Microsoft.ServiceModel.Web.WebHttpBehavior3"/>.</remarks>
        protected override void OnOpening()
        {
            base.OnOpening();
            foreach (ServiceEndpoint endpoint in Description.Endpoints)
            {
                WebHttpBehavior webHttpBehavior = endpoint.Behaviors.Find<WebHttpBehavior>();
                if (webHttpBehavior != null)
                {
                    endpoint.Behaviors.Remove(webHttpBehavior);
                    endpoint.Behaviors.Add(new WebHttpBehavior3());
                }
            }
        }
    }
}
