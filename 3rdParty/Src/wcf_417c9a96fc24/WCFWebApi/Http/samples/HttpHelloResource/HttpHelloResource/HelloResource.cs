using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using Microsoft.ApplicationServer.Http;
using Microsoft.ServiceModel.Web;

namespace HttpHelloResource
{
    using System.Net;
    using System.Net.Http;
    using System.Net.Http.Headers;
    using System.ServiceModel;
    using System.ServiceModel.Web;
    using System.Text;
    using System.Json;

    // Illustrates how to create a service that works with raw http messages

    [ServiceContract]
    public class HelloResource
    {
        //for testing
        public static void Initialize(IList<string> peopleToSayHelloTo)
        {
            HelloResource.peopleToSayHelloTo = peopleToSayHelloTo;
        }

        private static IList<string> peopleToSayHelloTo = new List<string>();

        [WebGet(UriTemplate = "")]
        public HttpResponseMessage Get()
        {
            var body = string.Format("Hello {0}", string.Join(",", peopleToSayHelloTo));
            var response = new HttpResponseMessage();
            response.Content = new StringContent(body, Encoding.UTF8, "text/plain");
            return response;
        }

        //The post method works directly with raw http requests and responses
        [WebInvoke(Method = "POST", UriTemplate = "")]
        public HttpResponseMessage Post(HttpRequestMessage request)
        {
            var body = request.Content.ReadAsString();
            dynamic formContent = FormUrlEncodedExtensions.ParseFormUrlEncoded(body);
            var person = (string)formContent.person;
            peopleToSayHelloTo.Add(person);
            var response = new HttpResponseMessage();
            response.Content = new StringContent(string.Format("Added {0}", person), Encoding.UTF8, "text/plain");
            response.StatusCode = HttpStatusCode.Created;
            return response;
        }
    }
}