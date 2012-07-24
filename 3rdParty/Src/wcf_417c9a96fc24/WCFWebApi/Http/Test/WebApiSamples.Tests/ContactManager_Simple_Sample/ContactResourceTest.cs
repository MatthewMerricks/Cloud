using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace WebApiSamples.Tests.ContactManager_Simple_Sample
{
    using System.Net;

    using ContactManager_Simple;

    using Microsoft.ApplicationServer.Http.Dispatcher;
    using Microsoft.VisualStudio.TestTools.UnitTesting;

    [TestClass]
    public class ContactResourceTest
    {
        private const string TestContactName = "Ron Jacobs";
        private int TestContactID = 1;
        private List<Contact> contacts = new List<Contact>();

        [TestMethod]
        public void WhenGettingAContactThenContactIsReturned()
        {
            var resource = this.GetContactsResourceWithSampleContact();
            var response = resource.Get(TestContactID);
            Assert.AreEqual(TestContactName, response.Content.ReadAs().Name);
        }

        [TestMethod]
        public void WhenGettingAContactThenExpirationIsSet()
        {
            var resource = this.GetContactsResourceWithSampleContact();
            var response = resource.Get(TestContactID);
            Assert.IsNotNull(response.Content.Headers.Expires);
        }

        [TestMethod]
        public void WhenGettingAContactAndIsIsMissingThenHttpResponseExceptionIsThrown()
        {
            var resource = this.GetContactsResourceWithSampleContact();
            this.Throws<HttpResponseException>(() => resource.Get(2));
        }

        [TestMethod]
        public void WhenGettingAContactAndIsIsMissingThenStatusCodeIsNotFound()
        {
            var resource = this.GetContactsResourceWithSampleContact();
            HttpResponseException exception = null;
            try
            {
                resource.Get(2);
            }
            catch (HttpResponseException ex)
            {
                exception = ex;
            }
            Assert.AreEqual(HttpStatusCode.NotFound, exception.Response.StatusCode);
        }

        private ContactResource GetContactsResourceWithSampleContact()
        {
            this.contacts.Add(new Contact{Name=TestContactName, ContactId=1});
            var repo = new ContactRepository(this.contacts);
            return new ContactResource(repo);
        }
    }
}
