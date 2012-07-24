// <copyright>
//   Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>

using Microsoft.ApplicationServer.Http.Activation;

namespace QueryableSample
{
    using System;
    using System.ServiceModel.Activation;
    using System.Web.Routing;

    public class Global : System.Web.HttpApplication
    {
        private void Application_Start(object sender, EventArgs e)
        {
            // setting up contacts services
            RouteTable.Routes.MapServiceRoute<ContactsResource>("contacts");
        }
    }
}
