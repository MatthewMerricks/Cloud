using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http.Headers;
using System.Web;
using Microsoft.ApplicationServer.Http;
using System.IO;

namespace ContactManager_Advanced
{
    public class CalendarFormatter : MediaTypeFormatter
    {
        public CalendarFormatter()
        {
            this.SupportedMediaTypes.Add(new MediaTypeHeaderValue("text/calendar"));
        }

        public override object OnReadFromStream(Type type, System.IO.Stream stream, System.Net.Http.Headers.HttpContentHeaders contentHeaders)
        {
            throw new NotImplementedException();
        }

        public override void OnWriteToStream(Type type, object value, System.IO.Stream stream, System.Net.Http.Headers.HttpContentHeaders contentHeaders, System.Net.TransportContext context)
        {
            var singleContact = value as Contact;
            if (singleContact != null)
            {
                WriteEvent(singleContact, stream);
            }
        }

        protected override bool OnCanReadType(Type type)
        {
            return false;
        }

        protected override bool OnCanWriteType(Type type)
        {
            return (type == typeof (Contact));
        }

        private void WriteEvent(Contact contact, Stream stream)
        {
            var dateFormat = "yyyyMMddTHHmmssZ";
            var eventDate = DateTime.Now.ToUniversalTime().AddDays(2).AddHours(4);
            var writer = new StreamWriter(stream);
            writer.WriteLine("BEGIN:VCALENDAR");
            writer.WriteLine("VERSION:2.0");
            writer.WriteLine("BEGIN:VEVENT");
            writer.WriteLine(string.Format("UID:{0}", contact.Email));
            writer.WriteLine(string.Format("DTSTAMP:{0}", DateTime.Now.ToUniversalTime().ToString(dateFormat)));
            writer.WriteLine(string.Format("DTSTART:{0}", eventDate.ToString(dateFormat)));
            writer.WriteLine(string.Format("DTEND:{0}", eventDate.AddHours(1).ToString(dateFormat)));
            writer.WriteLine("SUMMARY:Discuss WCF Web API");
            writer.WriteLine("END:VEVENT");
            writer.WriteLine("END:VCALENDAR");
            writer.Flush();
        }
    }
}