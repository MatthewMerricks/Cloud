﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http.Headers;
using System.Web;
using Microsoft.ApplicationServer.Http;

namespace ContactManager_Advanced
{
    using System.IO;

    public class VCardFormatter : MediaTypeFormatter
    {
        public VCardFormatter()
        {
            this.SupportedMediaTypes.Add(new MediaTypeHeaderValue("text/directory"));
        }

        public override void OnWriteToStream(Type type, object value, Stream stream, HttpContentHeaders contentHeaders, System.Net.TransportContext context)
        {
            var contacts = value as IEnumerable<Contact>;
            if (contacts != null)
            {
                foreach (var contact in contacts)
                {
                    WriteContact(contact, stream);
                }
                return;
            }

            var singleContact = value as Contact;
            if (singleContact != null)
            {
                WriteContact(singleContact, stream);
            }
        }

        private void WriteContact(Contact contact, Stream stream)
        {
            var writer = new StreamWriter(stream);
            writer.WriteLine("BEGIN:VCARD");
            writer.WriteLine(string.Format("FN:{0}", contact.Name));
            writer.WriteLine(string.Format("ADR;TYPE=HOME;{0};{1};{2}", contact.Address, contact.City, contact.Zip));
            writer.WriteLine(string.Format("EMAIL;TYPE=PREF,INTERNET:{0}", contact.Email));
            writer.WriteLine("END:VCARD");
            writer.Flush();
        }

        public override object OnReadFromStream(Type type, Stream stream, HttpContentHeaders contentHeaders)
        {
            throw new NotImplementedException();
        }

        protected override bool OnCanReadType(Type type)
        {
            return false;
        }
    }
}