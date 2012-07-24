using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace WebApiSamples.Tests.ContactManager_Simple_Sample
{
    using System.Net;
    using System.Net.Http;
    using System.Net.Http.Headers;

    using Microsoft.ApplicationServer.Http;
    using Microsoft.VisualStudio.TestTools.UnitTesting;
    using ContactManager_Simple;

    [TestClass]
    public class ContactManager_Simple_AcceptanceTests
    {
        private string contactsUri = "http://localhost:8080/contacts";
        private string contactUri = "http://localhost:8080/contact";
        private string contactGetUri = "http://localhost:8080/contact/1";
        private const string TestContactName = "Ron Jacobs";
        private const string TestPostedContactName = "Jeff Handley";
        private List<Contact> contacts = new List<Contact>();
        private HttpContent xmlPostContactContent = new StringContent(string.Format("<Contact><Name>{0}</Name></Contact>", TestPostedContactName),  Encoding.UTF8, "application/xml");
        private HttpContent jsonPostContactContent = new StringContent(string.Format("{{\"Name\":\"{0}\"}}", TestPostedContactName), Encoding.UTF8, "application/json");
        private HttpContent formUrlEncodedContactContent = new StringContent(string.Format("Name=Jeff+Handley"), Encoding.UTF8, "application/x-www-form-urlencoded");

        [TestMethod]
        public void WhenGettingContactsWithJsonAcceptHeaderThenResponseIsJson()
        {
            HttpResponseMessage response = null;
            using (var host = new HttpServiceHost(typeof(ContactsResource), contactsUri))
            {
                host.Open();
                var client = new HttpClient();
                client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
                response = client.Get(contactsUri);
            }
            response.HasContentWithMediaType("application/json");
        }

        [TestMethod]
        public void WhenGettingContactsAsJsonThenCanReadContacts()
        {
            Initialize();
            HttpResponseMessage response = null;
            using (var host = new HttpServiceHost(typeof(ContactsResource), contactsUri))
            {
                host.Open();
                var client = new HttpClient();
                client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
                response = client.Get(contactsUri);
            }
            var readContacts = response.Content.ReadAs<Contact[]>();
            Assert.AreEqual(1, readContacts.Count());
            Assert.AreEqual(TestContactName, readContacts.First().Name);
        }

        [TestMethod]
        public void WhenGettingContactsWithXmlAcceptHeaderThenResponseIsXml()
        {
            HttpResponseMessage response = null;
            using (var host = new HttpServiceHost(typeof(ContactsResource), contactsUri))
            {
                host.Open();
                var client = new HttpClient();
                client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/xml"));
                response = client.Get(contactsUri);
            }
            response.HasContentWithMediaType("application/xml");
        }

        [TestMethod]
        public void WhenGettingContactsAsXmlThenCanReadContacts()
        {
            Initialize();
            HttpResponseMessage response = null;
            using (var host = new HttpServiceHost(typeof(ContactsResource), contactsUri))
            {
                host.Open();
                var client = new HttpClient();
                client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/xml"));
                response = client.Get(contactsUri);
            }
            var readContacts = response.Content.ReadAs<Contact[]>();
            Assert.AreEqual(1, readContacts.Count());
            Assert.AreEqual(TestContactName, readContacts.First().Name);
        }

        [TestMethod]
        public void WhenGettingContactWithJsonAcceptHeaderThenResponseIsJson()
        {
            HttpResponseMessage response = null;
            using (var host = new HttpServiceHost(typeof(ContactResource), contactUri))
            {
                host.Open();
                var client = new HttpClient();
                client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
                response = client.Get(contactGetUri);
            }
            response.HasContentWithMediaType("application/json");

        }

        [TestMethod]
        public void WhenGettingContactWithXmlAcceptHeaderThenResponseIsXml()
        {
            HttpResponseMessage response = null;
            using (var host = new HttpServiceHost(typeof(ContactResource), contactUri))
            {
                host.Open();
                var client = new HttpClient();
                client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/xml"));
                response = client.Get(contactGetUri);
            }
            response.HasContentWithMediaType("application/xml");
        }

        [TestMethod]
        public void WhenPostingContactThenResponseStatusCodeIsCreated()
        {
            HttpResponseMessage response = null;
            using (var host = new HttpServiceHost(typeof(ContactsResource), contactsUri))
            {
                host.Open();
                var client = new HttpClient();
                response = client.Post(contactsUri, this.xmlPostContactContent);
            }
            Assert.AreEqual(HttpStatusCode.Created, response.StatusCode);
        }

        [TestMethod]
        public void WhenPostingContactAsXmlThenContactIsAddedToRepository()
        {
            this.Initialize();
            HttpResponseMessage response = null;
            using (var host = new HttpServiceHost(typeof(ContactsResource), contactsUri))
            {
                host.Open();
                var client = new HttpClient();
                response = client.Post(contactsUri, this.xmlPostContactContent);
            }
            Assert.AreEqual(TestPostedContactName, this.contacts.Last().Name);         
            
        }

        [TestMethod]
        public void WhenPostingContactAsJsonThenContactIsAddedToRepository()
        {
            this.Initialize();
            HttpResponseMessage response = null;
            using (var host = new HttpServiceHost(typeof(ContactsResource), contactsUri))
            {
                host.Open();
                var client = new HttpClient();
                response = client.Post(contactsUri, this.jsonPostContactContent);
            }
            Assert.AreEqual(TestPostedContactName, this.contacts.Last().Name);         
        }

        [TestMethod]
        public void WhenPostingContactAsFormEncodedThenContactIsAddedToRepository()
        {
            this.Initialize();
            HttpResponseMessage response = null;
            using (var host = new HttpServiceHost(typeof(ContactsResource), contactsUri))
            {
                host.Open();
                var client = new HttpClient();
                response = client.Post(contactsUri, this.formUrlEncodedContactContent);
            }
            Assert.AreEqual(TestPostedContactName, this.contacts.Last().Name);         
           
        }

        private void Initialize()
        {
           contacts.Add(new Contact { Name = TestContactName, ContactId = 1 });
           ContactRepository.Initialize(contacts);
        }
    }
}
