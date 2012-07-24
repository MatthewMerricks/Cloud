using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace ContactManager_Advanced
{
    using System.Collections;
    using System.Data.OData;
    using System.IO;
    using System.Net.Http.Headers;

    using Microsoft.ApplicationServer.Http;

    public class ContactFeedFormatter : MediaTypeFormatter
    {
        private string rootUri;

        public ContactFeedFormatter(string rootUri)
        {
            this.rootUri = rootUri;
            this.SupportedMediaTypes.Add(new MediaTypeHeaderValue("application/atom+xml"));
        }

        public override object OnReadFromStream(Type type, System.IO.Stream stream, System.Net.Http.Headers.HttpContentHeaders contentHeaders)
        {
            throw new NotImplementedException();
        }

        public override void OnWriteToStream(Type type, object value, System.IO.Stream stream, System.Net.Http.Headers.HttpContentHeaders contentHeaders, System.Net.TransportContext context)
        {
            var contacts = (IEnumerable<Contact>) value;
            var writer = new ContactFeedWriter(this.rootUri);
            writer.Write(stream, contacts);
        }


        protected override bool OnCanWriteType(Type type)
        {
            return typeof(IEnumerable<Contact>).IsAssignableFrom(type);
        }

        protected override bool OnCanReadType(Type type)
        {
            return false;
        }
    }
}