// <copyright>
//   Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>

namespace CalendarEntry
{
    using System;
    using System.ServiceModel.Activation;
    using System.Web.Routing;
    using Microsoft.ServiceModel.Activation;
    
    public class Global : System.Web.HttpApplication
    {
        protected void Application_Start(object sender, EventArgs e)
        {
            RouteTable.Routes.Add(new ServiceRoute("calendar", new WebServiceHostFactory3(), typeof(CalendarResource)));
            RouteTable.Routes.Add(new ServiceRoute("contacts", new WebServiceHostFactory3(), typeof(ContactsResource)));
        }
    }
}