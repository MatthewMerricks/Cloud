// <copyright>
//   Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>

using System.Collections.Generic;
using Microsoft.ApplicationServer.Http.Activation;
using Microsoft.ApplicationServer.Http.Dispatcher;

namespace ContactManager_Advanced
{
    using System;
    using System.ComponentModel.Composition.Hosting;
    using System.ServiceModel;
    using System.ServiceModel.Activation;
    using System.ServiceModel.Web;
    using System.Threading.Tasks;
    using System.Web.Routing;

    using Microsoft.ApplicationServer.Http.Channels;
    using Microsoft.ApplicationServer.Http.Description;
    using Microsoft.ApplicationServer.Http;

    public class Global : System.Web.HttpApplication
    {
        protected void Application_Start(object sender, EventArgs e)
        {
            // use MEF for providing instances
            var catalog = new AssemblyCatalog(typeof(Global).Assembly);
            var container = new CompositionContainer(catalog);
            var config = HttpHostConfiguration.Create().
                AddFormatters(
                    new ContactPngFormatter(),
                    new ContactFeedFormatter("http://localhost:9000/Contact"),
                    new VCardFormatter(),
                    new CalendarFormatter()).
                SetResourceFactory(new MefResourceFactory(container)).
                AddMessageHandlers(typeof (LoggingChannel), typeof (UriFormatExtensionMessageChannel));
            
            SetMappings();

            RouteTable.Routes.MapServiceRoute<ContactResource>("Contact", config);
            RouteTable.Routes.MapServiceRoute<ContactsResource>("Contacts", config);
        }

        public void SetMappings()
        {
            var mappings = new List<UriExtensionMapping>();
            mappings.AddMapping("xml", "application/xml");
            mappings.AddMapping("json", "application/json");
            mappings.AddMapping("png", "image/png");
            mappings.AddMapping("odata", "application/atom+xml");
            mappings.AddMapping("vcf", "text/directory");
            mappings.AddMapping("ics", "text/calendar");

            this.SetUriExtensionMappings(mappings);
        }

        public List<MediaTypeFormatter> GetFormatters()
        {
            var formatters = new List<MediaTypeFormatter>();
            formatters.Add(new ContactPngFormatter());
            formatters.Add(new ContactFeedFormatter("http://localhost:9000/Contact"));
            formatters.Add(new VCardFormatter());
            formatters.Add(new CalendarFormatter());
            return formatters;
        }

    }
}