// <copyright>
//   Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>

namespace WeatherWidget
{
    using System;
    using System.ServiceModel;
    using System.ServiceModel.Activation;
    using System.ServiceModel.Description;
    using Microsoft.ServiceModel.Web;

    public class WebServiceHostFactory3WithJsonP : ServiceHostFactory
    {
        protected override ServiceHost CreateServiceHost(Type serviceType, Uri[] baseAddresses)
        {
            WebServiceHost3 wsh = new WebServiceHost3(serviceType, baseAddresses);
            wsh.Opening += new EventHandler(this.Wsh_Opening);
            return wsh;
        }

        private void Wsh_Opening(object sender, EventArgs e)
        {
            WebServiceHost3 host = sender as WebServiceHost3;
            foreach (ServiceEndpoint endpoint in host.Description.Endpoints)
            {
                if (endpoint.Binding is WebHttpBinding)
                {
                    WebHttpBinding binding = endpoint.Binding as WebHttpBinding;
                    binding.Security = new WebHttpSecurity { Mode = WebHttpSecurityMode.None };
                    binding.CrossDomainScriptAccessEnabled = true;
                }
            }
        }
    }
}