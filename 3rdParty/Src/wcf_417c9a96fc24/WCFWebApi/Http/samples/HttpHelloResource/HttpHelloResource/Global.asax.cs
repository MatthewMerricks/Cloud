using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Security;
using System.Web.SessionState;
using Microsoft.ApplicationServer.Http.Activation;

namespace HttpHelloResource
{
    using System.Collections;
    using System.ServiceModel.Description;
    using System.ServiceModel.Dispatcher;
    using System.Web.Routing;

    public class Global : System.Web.HttpApplication
    {
        protected void Application_Start(object sender, EventArgs e)
        {
            RouteTable.Routes.MapServiceRoute<HelloResource>("Hello");
        }
    }
}