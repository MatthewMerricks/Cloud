using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace ContactManager_Advanced
{
    using System.Data.OData;
    using System.IO;

    public class ContactFeedWriter
    {
        private string rootUri;

        public ContactFeedWriter(string rootUri)
        {
            this.rootUri = rootUri;
        }

        public void Write(Stream stream, IEnumerable<Contact> contacts)
        {
            var message = new ODataStreamResponseMessage(stream);
            var settings = new ODataWriterSettings { Indent = true, CheckCharacters = false, BaseUri = new Uri(this.rootUri), Version = ODataVersion.V3 };
            settings.SetContentType(ODataFormat.Atom);
            var messageWriter = new ODataMessageWriter(message, settings);
            var writerTask = messageWriter.CreateODataFeedWriterAsync();
            writerTask.Wait();
            using (var writer = writerTask.Result)
            {
                writer.WriteStart(
                    new ODataFeed()
                    {
                        Count = contacts.Count(),
                        Id = "Contacts"
                    });

                foreach (var contact in contacts)
                {
                    var entry = new ODataEntry()
                    {
                        Id = string.Format("urn:Contacts(\"{0}\"", contact.ContactId),
                        EditLink = new Uri(string.Format("{1}", this.rootUri, contact.ContactId), UriKind.Relative),
                        TypeName = "Contacts.Contact",
                        Properties =
                            new List<ODataProperty>()
                                    {
                                        new ODataProperty() { Value = contact.ContactId, Name = "ContactId" },
                                        new ODataProperty() { Value = contact.Name, Name = "Name" },
                                        new ODataProperty() { Value = contact.Address, Name = "Address" },
                                        new ODataProperty() { Value = contact.City, Name = "City" },
                                        new ODataProperty() { Value = contact.State, Name = "State" },
                                        new ODataProperty() { Value = contact.Zip, Name = "Zip" },
                                        new ODataProperty() { Value = contact.Email, Name = "Email" },
                                        new ODataProperty() { Value = contact.Twitter, Name = "Twitter" }
                                    }
                    };
                    writer.WriteStart(entry);
                    writer.WriteEnd();
                }
                writer.WriteEnd();
                writer.FlushAsync().Wait();
            }
        }

    }
}