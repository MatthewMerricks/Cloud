namespace WebApiSamples.Tests.HttpHelloResource_Sample
{
    using System.Collections.Generic;
    using System.Net.Http;
    using Microsoft.VisualStudio.TestTools.UnitTesting;

    using System.Net;

    using HttpHelloResource;

    [TestClass]
    public class HelloResourceTest
    {
        private IList<string> peopleToSayHelloTo = new List<string>();

        [TestMethod]
        public void WhenPostingAPersonThenPersonIsStored()
        {
            var request = this.GetPostRequest();
            var resource = new HelloResource();
            resource.Post(request);
            Assert.IsTrue(peopleToSayHelloTo.Contains("Glenn"));
        }
    
        [TestMethod]
        public void WhenPostingAPersonThenResponseIsText()
        {
            var request = GetPostRequest();
            var resource = new HelloResource();
            var response = resource.Post(request);
            response.HasContentWithMediaType("text/plain");
        }

        [TestMethod]
        public void WhenPostingAPersonThenResponseStatusCodeIsCreated()
        {
            var request = GetPostRequest();
            var resource = new HelloResource();
            var response = resource.Post(request);
            Assert.AreEqual(HttpStatusCode.Created, response.StatusCode);
        }

        [TestMethod]
        public void WhenPostingAPersonThenResponseIndicatesPersonWasAdded()
        {
            var request = this.GetPostRequest();
            var resource = new HelloResource();
            var response = resource.Post(request);
            Assert.AreEqual("Added Glenn", response.Content.ReadAsString());
        }

        [TestMethod]
        public void WhenGettingThenReturnsListOfPeopleAdded()
        {
            var resource = this.GetHelloResourceWithNameAdded();
            var response = resource.Get();
            Assert.AreEqual("Hello Glenn", response.Content.ReadAsString());
        }

        [TestMethod]
        public void WhenGettingThenResponseIsText()
        {
            var resource = this.GetHelloResourceWithNameAdded();
            var response = resource.Get();
            response.HasContentWithMediaType("text/plain");
        }

        private HttpRequestMessage GetPostRequest()
        {
            HelloResource.Initialize(peopleToSayHelloTo);
            var request = new HttpRequestMessage();
            request.Content = new StringContent("person=Glenn");
            return request;
        }

        private HelloResource GetHelloResourceWithNameAdded()
        {
            this.peopleToSayHelloTo.Add("Glenn");
            HelloResource.Initialize(peopleToSayHelloTo);
            return new HelloResource();
        }

    }
}
