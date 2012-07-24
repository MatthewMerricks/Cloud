using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace WebApiSamples.Tests.ContactManager_Simple_Sample
{
    using System.Net;
    using ContactManager_Simple;
    using Microsoft.ApplicationServer.Http;
    using Microsoft.VisualStudio.TestTools.UnitTesting;

    [TestClass]
    public class ContactsResourceTest
    {
        private const string TestContactName = "Ron Jacobs";
        private List<Contact> contacts = new List<Contact>();

        [TestMethod]
        public void WhenGettingThenContactsAreReturned()
        {
            var contacts = new List<Contact>();
            contacts.Add(new Contact {Name=TestContactName});
            var repo = new ContactRepository(contacts);
            var resource = new ContactsResource(repo);
            var returnedContacts = resource.Get();
            Assert.AreEqual(TestContactName, returnedContacts.First().Name);
        }

        [TestMethod]
        public void WhenPostingThenContactIsAdded()
        {
            var resource = this.GetContactsResource();
            resource.Post(this.GetContact());
            Assert.AreEqual(TestContactName, contacts.First().Name);
        }

        [TestMethod]
        public void WhenPostingThenContactIDIsSet()
        {
            var resource = this.GetContactsResource();
            resource.Post(this.GetContact());
            var contact = this.contacts.First();
            Assert.AreEqual(1, contact.ContactId);
        }

        [TestMethod]
        public void WhenPostingThenStatusCodeIsSetToCreated()
        {
            var resource = this.GetContactsResource();
            var response = resource.Post(this.GetContact());
            Assert.AreEqual(HttpStatusCode.Created, response.StatusCode);
        }

        [TestMethod]
        public void WhenPostingThenContactIsReturned()
        {
            var resource = this.GetContactsResource();
            var response = resource.Post(this.GetContact());
            var contact = response.Content.ReadAs();
            Assert.AreEqual(TestContactName, contact.Name);
        }

        private ContactsResource GetContactsResource()
        {
            var repo = new ContactRepository(this.contacts);
            return new ContactsResource(repo);
        }

        private Contact GetContact()
        {
            return new Contact { Name = TestContactName };
        }


    }
}
