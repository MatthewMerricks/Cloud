namespace WebApiSamples.Tests.HttpHelloResource_Sample
{
    using System.Collections.Generic;
    using System.Text;

    using HttpHelloResource;
    using Microsoft.VisualStudio.TestTools.UnitTesting;
    using System.Net.Http;
    using Microsoft.ApplicationServer.Http;

    [TestClass]
    public class HelloResourceAcceptanceTest
    {
        private string hostUri = "http://localhost:8080/hello";

        [TestMethod]
        public void WhenPostingAPersonThenResponseIndicatesPersonWasAdded()
        {
            HelloResource.Initialize(new List<string>());
            using (var host = new HttpServiceHost(typeof(HelloResource), this.hostUri))
            {
                host.Open();
                var client = new HttpClient();
                var response = client.Post(this.hostUri, new StringContent("person=Glenn", Encoding.UTF8, "application/x-www-form-urlencoded"));
                Assert.AreEqual("Added Glenn", response.Content.ReadAsString());
            }
        }

        [TestMethod]
        public void WhenGettingThenReturnsListOfPeopleAdded()
        {
            var peopleToSayHelloTo = new List<string>();
            peopleToSayHelloTo.Add("Glenn");
            HelloResource.Initialize(peopleToSayHelloTo);
            using (var host = new HttpServiceHost(typeof(HelloResource), this.hostUri))
            {
                host.Open();
                var client = new HttpClient();
                var response = client.Get(this.hostUri);
                Assert.AreEqual("Hello Glenn", response.Content.ReadAsString());
                host.Close();
            }
        }
    }
}
